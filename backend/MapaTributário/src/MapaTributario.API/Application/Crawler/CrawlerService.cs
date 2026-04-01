using System.Diagnostics;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using MapaTributario.API.Infrastructure.External;
using Microsoft.Extensions.Logging;

namespace MapaTributario.API.Application.Crawler;

public class CrawlerService : ICrawlerService
{
    private readonly IExecucaoCrawlerRepository _execucaoRepository;
    private readonly IFilaProcessamentoRepository _filaRepository;
    private readonly IMunicipioRepository _municipioRepository;
    private readonly IServicoRepository _servicoRepository;
    private readonly IAliquotaRepository _aliquotaRepository;
    private readonly INfseApiClient _nfseApiClient;
    private readonly IRateLimiter _rateLimiter;
    private readonly ICircuitBreaker _circuitBreaker;
    private readonly ICertificateProtection _certificateProtection;
    private readonly ICrawlerExecutionGuard _executionGuard;
    private readonly ILogger<CrawlerService> _logger;

    private static readonly string[] ProbeServiceCodes = new[]
    {
        "01.01.01", "07.02.01", "14.01.01", "17.01.01", "25.01.01"
    };

    private const int MaxRetries = 3;
    private const int EarlyStopThreshold = 9;
    private const int BatchSize = 50;

    public CrawlerService(
        IExecucaoCrawlerRepository execucaoRepository,
        IFilaProcessamentoRepository filaRepository,
        IMunicipioRepository municipioRepository,
        IServicoRepository servicoRepository,
        IAliquotaRepository aliquotaRepository,
        INfseApiClient nfseApiClient,
        IRateLimiter rateLimiter,
        ICircuitBreaker circuitBreaker,
        ICertificateProtection certificateProtection,
        ICrawlerExecutionGuard executionGuard,
        ILogger<CrawlerService> logger)
    {
        _execucaoRepository = execucaoRepository;
        _filaRepository = filaRepository;
        _municipioRepository = municipioRepository;
        _servicoRepository = servicoRepository;
        _aliquotaRepository = aliquotaRepository;
        _nfseApiClient = nfseApiClient;
        _rateLimiter = rateLimiter;
        _circuitBreaker = circuitBreaker;
        _certificateProtection = certificateProtection;
        _executionGuard = executionGuard;
        _logger = logger;
    }

    public bool EmExecucao => _executionGuard.IsRunning;

    public async Task<ExecucaoCrawler> ExecutarAsync(
        TipoExecucao tipo,
        bool forcarReprocessamento = false,
        IReadOnlyList<string>? filtroUfs = null,
        CancellationToken cancellationToken = default)
    {
        if (!_executionGuard.TryAcquire())
        {
            throw new InvalidOperationException("Uma execucao ja esta em andamento");
        }

        _certificateProtection.Reset();
        _circuitBreaker.Reset();

        ExecucaoCrawler execucao = ExecucaoCrawler.Create(tipo);

        try
        {
            await _execucaoRepository.CreateAsync(execucao);

            // Revert orphan "processando" items from previous interrupted execution
            await _filaRepository.RevertProcessingTopendingAsync();

            string competencia = GetCompetenciaAtual();

            // Phase 1: Discover active municipalities via convenio endpoint
            List<Municipio> municipiosAtivos = await FaseConvenioAsync(filtroUfs, cancellationToken);

            if (municipiosAtivos.Count == 0)
            {
                _logger.LogWarning("No active municipalities found. Ending execution");
                execucao.Finalizar(StatusExecucao.Concluido);
                await _execucaoRepository.UpdateAsync(execucao);
                return execucao;
            }

            // Phase 2: Probe municipalities
            List<Municipio> municipiosComDados = await FaseProbeAsync(
                municipiosAtivos, competencia, cancellationToken);

            // Get services
            IReadOnlyList<Servico> servicos = await _servicoRepository.GetAllAsync();

            execucao.SetTotais(municipiosComDados.Count, servicos.Count);
            await _execucaoRepository.UpdateAsync(execucao);

            // Generate queue (incremental unless forced)
            await GerarFilaAsync(
                municipiosComDados,
                servicos,
                competencia,
                execucao.Id,
                forcarReprocessamento,
                cancellationToken);

            // Phase 3: Process queue
            await ProcessarFilaAsync(execucao, competencia, cancellationToken);

            // Finalize
            StatusExecucao statusFinal = execucao.Erros > 0
                ? StatusExecucao.FalhaParcial
                : StatusExecucao.Concluido;

            execucao.Finalizar(statusFinal);
            await _execucaoRepository.UpdateAsync(execucao);

            _logger.LogInformation(
                "Crawler execution completed. Status={Status}, Processed={Processed}, Errors={Errors}",
                statusFinal, execucao.Processados, execucao.Erros);

            return execucao;
        }
        catch (OperationCanceledException)
        {
            execucao.Finalizar(StatusExecucao.Falha);
            await _execucaoRepository.UpdateAsync(execucao);
            _logger.LogWarning("Crawler execution was cancelled");
            return execucao;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Crawler execution failed with unexpected error");
            execucao.IncrementarErros($"Erro fatal: {ex.Message}");
            execucao.Finalizar(StatusExecucao.Falha);
            await _execucaoRepository.UpdateAsync(execucao);
            return execucao;
        }
        finally
        {
            _executionGuard.Release();
        }
    }

    internal async Task<List<Municipio>> FaseConvenioAsync(
        IReadOnlyList<string>? filtroUfs,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Phase 1: Discovering municipalities via convenio endpoint");

        List<Municipio> todos = new();
        string[] todasUfs = new[]
        {
            "AC","AL","AM","AP","BA","CE","DF","ES","GO","MA","MG","MS","MT",
            "PA","PB","PE","PI","PR","RJ","RN","RO","RR","RS","SC","SE","SP","TO"
        };

        IEnumerable<string> ufsParaProcessar = filtroUfs is { Count: > 0 }
            ? filtroUfs.Select(u => u.ToUpperInvariant()).Where(u => todasUfs.Contains(u))
            : todasUfs;

        if (filtroUfs is { Count: > 0 })
        {
            _logger.LogInformation("Filtering execution to UFs: {Ufs}", string.Join(", ", ufsParaProcessar));
        }

        foreach (string uf in ufsParaProcessar)
        {
            IReadOnlyList<Municipio> porUf = await _municipioRepository.GetByUfAsync(uf);
            todos.AddRange(porUf);
        }

        List<Municipio> ativos = new();

        foreach (Municipio municipio in todos)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_certificateProtection.ShouldHalt || _certificateProtection.BudgetExhausted)
            {
                break;
            }

            try
            {
                await _rateLimiter.WaitAsync(cancellationToken);
                await _circuitBreaker.WaitIfOpenAsync(cancellationToken);

                Stopwatch sw = Stopwatch.StartNew();
                Infrastructure.External.Contracts.ConvenioNfseResponse? convenio =
                    await _nfseApiClient.GetConvenioAsync(municipio.CodigoIbge, cancellationToken);
                sw.Stop();

                _certificateProtection.OnResponseReceived(200, sw.Elapsed.TotalSeconds);
                _circuitBreaker.RecordSuccess();
                await _certificateProtection.OnItemProcessedAsync(cancellationToken);

                if (convenio is not null && convenio.Ativo)
                {
                    ativos.Add(municipio);
                }
            }
            catch (HttpRequestException ex)
            {
                int statusCode = (int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError);
                _certificateProtection.OnResponseReceived(statusCode, 0);
                _circuitBreaker.RecordFailure();
                await _certificateProtection.OnItemProcessedAsync(cancellationToken);

                _logger.LogWarning(
                    "Failed to check convenio for municipality {CodigoIbge}: {Message}",
                    municipio.CodigoIbge, ex.Message);
            }
        }

        _logger.LogInformation("Phase 1 complete. {Active}/{Total} municipalities active",
            ativos.Count, todos.Count);

        return ativos;
    }

    internal async Task<List<Municipio>> FaseProbeAsync(
        List<Municipio> municipiosAtivos,
        string competencia,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Phase 2: Probing {Count} municipalities", municipiosAtivos.Count);

        List<Municipio> comDados = new();

        foreach (Municipio municipio in municipiosAtivos)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_certificateProtection.ShouldHalt || _certificateProtection.BudgetExhausted)
            {
                break;
            }

            bool temDados = false;

            foreach (string probeCode in ProbeServiceCodes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await _rateLimiter.WaitAsync(cancellationToken);
                    await _circuitBreaker.WaitIfOpenAsync(cancellationToken);

                    Stopwatch sw = Stopwatch.StartNew();
                    Infrastructure.External.Contracts.AliquotaNfseResponse? result =
                        await _nfseApiClient.GetAliquotaAsync(
                            municipio.CodigoIbge, probeCode, competencia, cancellationToken);
                    sw.Stop();

                    _certificateProtection.OnResponseReceived(result != null ? 200 : 404, sw.Elapsed.TotalSeconds);
                    _circuitBreaker.RecordSuccess();
                    await _certificateProtection.OnItemProcessedAsync(cancellationToken);

                    if (result is not null)
                    {
                        temDados = true;
                        break;
                    }
                }
                catch (HttpRequestException ex)
                {
                    int statusCode = (int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError);
                    _certificateProtection.OnResponseReceived(statusCode, 0);
                    _circuitBreaker.RecordFailure();
                    await _certificateProtection.OnItemProcessedAsync(cancellationToken);
                }
            }

            if (temDados)
            {
                comDados.Add(municipio);
            }
            else
            {
                _logger.LogDebug("Municipality {CodigoIbge} ({Nome}) marked as sem_dados_adn",
                    municipio.CodigoIbge, municipio.Nome);
            }
        }

        _logger.LogInformation("Phase 2 complete. {WithData}/{Total} municipalities have ADN data",
            comDados.Count, municipiosAtivos.Count);

        return comDados;
    }

    internal async Task GerarFilaAsync(
        List<Municipio> municipios,
        IReadOnlyList<Servico> servicos,
        string competencia,
        string execucaoId,
        bool forcarReprocessamento,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating work queue for {Municipios} municipalities x {Servicos} services",
            municipios.Count, servicos.Count);

        List<FilaProcessamento> novosItens = new();

        foreach (Municipio municipio in municipios)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (Servico servico in servicos)
            {
                if (!forcarReprocessamento)
                {
                    bool jaColetado = await _aliquotaRepository.ExistsAsync(
                        municipio.CodigoIbge, servico.CodigoTribNac, competencia);

                    if (jaColetado)
                    {
                        continue;
                    }
                }

                novosItens.Add(FilaProcessamento.Create(
                    municipio.CodigoIbge,
                    servico.CodigoTribNac,
                    competencia,
                    execucaoId));
            }
        }

        if (novosItens.Count > 0)
        {
            await _filaRepository.InsertManyAsync(novosItens);
        }

        _logger.LogInformation("Work queue generated with {Count} items", novosItens.Count);
    }

    internal async Task ProcessarFilaAsync(
        ExecucaoCrawler execucao,
        string competencia,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Phase 3: Processing work queue");

        // Track consecutive misses for early-stop per group (XX.XX.XX)
        Dictionary<string, int> consecutiveMissesByGroup = new();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_certificateProtection.ShouldHalt)
            {
                _logger.LogCritical("Certificate protection halt triggered. Stopping processing");
                break;
            }

            if (_certificateProtection.BudgetExhausted)
            {
                _logger.LogWarning("Daily budget exhausted. Stopping processing");
                break;
            }

            IReadOnlyList<FilaProcessamento> batch = await _filaRepository.GetPendingAsync(BatchSize);

            if (batch.Count == 0)
            {
                break;
            }

            foreach (FilaProcessamento item in batch)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_certificateProtection.ShouldHalt || _certificateProtection.BudgetExhausted)
                {
                    break;
                }

                // Early-stop check
                string group = ExtractGroup(item.CodigoServico);
                if (consecutiveMissesByGroup.TryGetValue(group, out int misses) && misses >= EarlyStopThreshold)
                {
                    item.MarcarConcluido();
                    await _filaRepository.UpdateStatusAsync(item);
                    execucao.IncrementarProcessados();
                    continue;
                }

                await ProcessarItemAsync(item, execucao, competencia, consecutiveMissesByGroup, cancellationToken);
            }

            await _execucaoRepository.UpdateAsync(execucao);
        }
    }

    internal async Task ProcessarItemAsync(
        FilaProcessamento item,
        ExecucaoCrawler execucao,
        string competencia,
        Dictionary<string, int> consecutiveMissesByGroup,
        CancellationToken cancellationToken)
    {
        item.MarcarProcessando();
        await _filaRepository.UpdateStatusAsync(item);

        try
        {
            await _rateLimiter.WaitAsync(cancellationToken);
            await _circuitBreaker.WaitIfOpenAsync(cancellationToken);

            Stopwatch sw = Stopwatch.StartNew();
            Infrastructure.External.Contracts.AliquotaNfseResponse? result =
                await _nfseApiClient.GetAliquotaAsync(
                    item.CodigoMunicipio, item.CodigoServico, competencia, cancellationToken);
            sw.Stop();

            _certificateProtection.OnResponseReceived(result != null ? 200 : 404, sw.Elapsed.TotalSeconds);
            _circuitBreaker.RecordSuccess();
            await _certificateProtection.OnItemProcessedAsync(cancellationToken);

            string group = ExtractGroup(item.CodigoServico);

            if (result is not null)
            {
                // Reset consecutive misses for this group
                consecutiveMissesByGroup[group] = 0;

                // Get municipality name for upsert
                Municipio? municipio = await _municipioRepository.GetByCodigoIbgeAsync(item.CodigoMunicipio);
                string nomeMunicipio = municipio?.Nome ?? item.CodigoMunicipio;

                Aliquota aliquota = Aliquota.Create(
                    item.CodigoMunicipio,
                    nomeMunicipio,
                    item.CodigoServico,
                    FormatServiceCode(item.CodigoServico),
                    result.DescricaoServico ?? string.Empty,
                    result.Aliquota,
                    competencia,
                    "API NFS-e Nacional");

                await _aliquotaRepository.UpsertAsync(aliquota);

                item.MarcarConcluido();
                await _filaRepository.UpdateStatusAsync(item);
                execucao.IncrementarProcessados();
            }
            else
            {
                // 404 - no data (not an error)
                if (consecutiveMissesByGroup.ContainsKey(group))
                {
                    consecutiveMissesByGroup[group]++;
                }
                else
                {
                    consecutiveMissesByGroup[group] = 1;
                }

                item.MarcarConcluido();
                await _filaRepository.UpdateStatusAsync(item);
                execucao.IncrementarProcessados();
            }
        }
        catch (HttpRequestException ex)
        {
            int statusCode = (int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError);
            _certificateProtection.OnResponseReceived(statusCode, 0);
            _circuitBreaker.RecordFailure();
            await _certificateProtection.OnItemProcessedAsync(cancellationToken);

            bool isRetryable = statusCode >= 500 || statusCode == 0;

            if (isRetryable && item.PodeRetentar(MaxRetries))
            {
                item.MarcarErro(ex.Message, MaxRetries);
            }
            else
            {
                item.MarcarErro(ex.Message, 0); // Force max retries reached
                execucao.IncrementarErros(
                    $"Municipio={item.CodigoMunicipio}, Servico={item.CodigoServico}: {ex.Message}");
            }

            await _filaRepository.UpdateStatusAsync(item);
        }
        catch (TaskCanceledException)
        {
            // Timeout
            if (item.PodeRetentar(MaxRetries))
            {
                item.MarcarErro("Timeout", MaxRetries);
            }
            else
            {
                item.MarcarErro("Timeout", 0);
                execucao.IncrementarErros(
                    $"Municipio={item.CodigoMunicipio}, Servico={item.CodigoServico}: Timeout");
            }

            await _filaRepository.UpdateStatusAsync(item);
            _circuitBreaker.RecordFailure();
        }
    }

    internal static string GetCompetenciaAtual()
    {
        DateTime now = DateTime.UtcNow;
        return new DateTime(now.Year, now.Month, 1).ToString("yyyy-MM-dd");
    }

    internal static string ExtractGroup(string codigoServico)
    {
        // Extract group XX.XX.XX from code like "01.01.01" or "01.01.01.001"
        string code = codigoServico.Replace(".", "");
        if (code.Length >= 6)
        {
            return $"{code[..2]}.{code[2..4]}.{code[4..6]}";
        }

        return codigoServico;
    }

    internal static string FormatServiceCode(string code)
    {
        string clean = code.Replace(".", "");
        if (clean.Length >= 6)
        {
            string formatted = $"{clean[..2]}.{clean[2..4]}.{clean[4..6]}";
            if (clean.Length > 6)
            {
                formatted += $".{clean[6..]}";
            }

            return formatted;
        }

        return code;
    }
}
