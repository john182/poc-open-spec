using System.Diagnostics;
using FluentResults;
using MapaTributario.API.Application.Errors;
using MapaTributario.API.Domain.Constants;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using MapaTributario.API.Infrastructure.External;
using MapaTributario.API.Infrastructure.External.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MapaTributario.API.Application.Crawler;

public class CrawlerService : ICrawlerService
{
    private readonly IExecucaoCrawlerRepository _execucaoRepository;
    private readonly IFilaProcessamentoRepository _filaRepository;
    private readonly IMunicipioRepository _municipioRepository;
    private readonly IServicoRepository _servicoRepository;
    private readonly IAliquotaRepository _aliquotaRepository;
    private readonly IConfiguracaoCrawlerRepository _configuracaoRepository;
    private readonly INfseApiClient _nfseApiClient;
    private readonly IRateLimiter _rateLimiter;
    private readonly ICircuitBreaker _circuitBreaker;
    private readonly ICertificateProtection _certificateProtection;
    private readonly ICrawlerExecutionGuard _executionGuard;
    private readonly ICertificadoStore _certificadoStore;
    private readonly string? _caminhoArquivoCertificado;
    private readonly ILogger<CrawlerService> _logger;

    // Configuração carregada do MongoDB no início de cada execução
    private ConfiguracaoCrawler _configuracao = ConfiguracaoCrawler.CriarPadrao();

    public CrawlerService(
        IExecucaoCrawlerRepository execucaoRepository,
        IFilaProcessamentoRepository filaRepository,
        IMunicipioRepository municipioRepository,
        IServicoRepository servicoRepository,
        IAliquotaRepository aliquotaRepository,
        IConfiguracaoCrawlerRepository configuracaoRepository,
        INfseApiClient nfseApiClient,
        IRateLimiter rateLimiter,
        ICircuitBreaker circuitBreaker,
        ICertificateProtection certificateProtection,
        ICrawlerExecutionGuard executionGuard,
        ICertificadoStore certificadoStore,
        IConfiguration configuration,
        ILogger<CrawlerService> logger)
    {
        _execucaoRepository = execucaoRepository;
        _filaRepository = filaRepository;
        _municipioRepository = municipioRepository;
        _servicoRepository = servicoRepository;
        _aliquotaRepository = aliquotaRepository;
        _configuracaoRepository = configuracaoRepository;
        _nfseApiClient = nfseApiClient;
        _rateLimiter = rateLimiter;
        _circuitBreaker = circuitBreaker;
        _certificateProtection = certificateProtection;
        _executionGuard = executionGuard;
        _certificadoStore = certificadoStore;
        _caminhoArquivoCertificado = configuration["NfseApi:CertificatePath"];
        _logger = logger;
    }

    public bool EmExecucao => _executionGuard.IsRunning;

    /// <summary>
    /// Verifica se existe certificado digital disponível (dinâmico via upload ou estático via arquivo).
    /// Sem certificado, as chamadas à API NFS-e falharão com 496/403.
    /// </summary>
    internal bool CertificadoDisponivel()
    {
        // Certificado dinâmico (upload via API)
        if (_certificadoStore.HasCertificate())
            return true;

        // Certificado estático (arquivo PFX configurado em NfseApi:CertificatePath)
        if (!string.IsNullOrEmpty(_caminhoArquivoCertificado) && File.Exists(_caminhoArquivoCertificado))
            return true;

        return false;
    }

    public async Task<Result<ExecucaoCrawler>> ExecutarAsync(
        TipoExecucao tipo,
        bool forcarReprocessamento = false,
        IReadOnlyList<string>? filtroUfs = null,
        CancellationToken cancellationToken = default)
    {
        if (!CertificadoDisponivel())
        {
            _logger.LogWarning(
                "Crawler não iniciado: nenhum certificado digital disponível. " +
                "Faça upload via POST /api/v1/crawler/certificado ou configure NfseApi:CertificatePath");
            return Result.Fail<ExecucaoCrawler>(new CertificadoNaoDisponivelError());
        }

        if (!_executionGuard.TryAcquire())
        {
            return Result.Fail<ExecucaoCrawler>(new ExecucaoEmAndamentoError());
        }

        // Carregar configuração do MongoDB
        _configuracao = await _configuracaoRepository.ObterAtualAsync()
            ?? ConfiguracaoCrawler.CriarPadrao();

        // Verificar se o crawler está ativo
        if (!_configuracao.Ativo)
        {
            _logger.LogWarning("Crawler desativado pela configuração. Abortando execução.");
            _executionGuard.Release();
            return Result.Fail<ExecucaoCrawler>(new CrawlerDesativadoError());
        }

        // Validar configuração carregada
        ValidarConfiguracao();

        _logger.LogInformation(
            "Configuração do crawler carregada (Id={Id}, TamanhoLoteMongo={Lote}, MaxItensParalelos={Paralelos})",
            _configuracao.Id ?? "padrao-em-memoria",
            _configuracao.TamanhoLoteMongo,
            _configuracao.MaxItensParalelos);

        _certificateProtection.Reset();
        _circuitBreaker.Reset();

        ExecucaoCrawler execucao = ExecucaoCrawler.Create(tipo);

        // Record which UFs are being processed
        execucao.SetUfsProcessadas(filtroUfs is { Count: > 0 } ? filtroUfs : UfsBrasil.Todas);

        try
        {
            await _execucaoRepository.CreateAsync(execucao);

            // Revert orphan "processando" items from previous interrupted execution
            await _filaRepository.RevertProcessingTopendingAsync();

            string competencia = GetCompetenciaAtual();

            // Phase 1: Discover active municipalities via convenio endpoint
            List<Municipio> municipiosAtivos = await FaseConvenioAsync(execucao, filtroUfs, cancellationToken);

            // Persistir progresso de UFs no banco
            await _execucaoRepository.UpdateAsync(execucao);

            if (municipiosAtivos.Count == 0)
            {
                _logger.LogWarning("No active municipalities found. Ending execution");
                execucao.Finalizar(StatusExecucao.Concluido);
                await _execucaoRepository.UpdateAsync(execucao);
                return Result.Ok(execucao);
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

            return Result.Ok(execucao);
        }
        catch (OperationCanceledException)
        {
            execucao.Finalizar(StatusExecucao.Falha);
            await _execucaoRepository.UpdateAsync(execucao);
            _logger.LogWarning("Crawler execution was cancelled");
            return Result.Ok(execucao);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Crawler execution failed with unexpected error");
            execucao.IncrementarErros($"Erro fatal: {ex.Message}");
            execucao.Finalizar(StatusExecucao.Falha);
            await _execucaoRepository.UpdateAsync(execucao);
            return Result.Ok(execucao);
        }
        finally
        {
            _executionGuard.Release();
        }
    }

    internal async Task<List<Municipio>> FaseConvenioAsync(
        ExecucaoCrawler execucao,
        IReadOnlyList<string>? filtroUfs,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Phase 1: Discovering municipalities via convenio endpoint");

        List<Municipio> todos = new();

        IEnumerable<string> ufsParaProcessar = filtroUfs is { Count: > 0 }
            ? filtroUfs.Select(u => u.ToUpperInvariant()).Where(u => UfsBrasil.Todas.Contains(u))
            : UfsBrasil.Todas;

        if (filtroUfs is { Count: > 0 })
        {
            _logger.LogInformation("Filtering execution to UFs: {Ufs}", string.Join(", ", ufsParaProcessar));
        }

        foreach (string uf in ufsParaProcessar)
        {
            execucao.IniciarProcessamentoUf(uf);

            IReadOnlyList<Municipio> porUf = await _municipioRepository.GetByUfAsync(uf);
            todos.AddRange(porUf);

            int municipiosUf = porUf.Count;
            execucao.FinalizarProcessamentoUf(uf, municipiosUf);
        }

        // Priorizar capitais: processar todas as capitais primeiro (de todas as UFs),
        // depois os demais municípios em ordem alfabética por UF e nome.
        List<Municipio> todosOrdenados = todos
            .OrderByDescending(m => m.EhCapital)
            .ThenBy(m => m.SiglaEstado)
            .ThenBy(m => m.Nome)
            .ToList();

        _logger.LogInformation(
            "Processing order: {Capitais} capitais first, then {Demais} remaining municipalities",
            todosOrdenados.Count(m => m.EhCapital),
            todosOrdenados.Count(m => !m.EhCapital));

        List<Municipio> ativos = new();

        foreach (Municipio municipio in todosOrdenados)
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
                ConvenioNfseResponse? convenio =
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

            foreach (string probeCode in _configuracao.CodigosSondagem)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await _rateLimiter.WaitAsync(cancellationToken);
                    await _circuitBreaker.WaitIfOpenAsync(cancellationToken);

                    Stopwatch sw = Stopwatch.StartNew();
                    // NfseApiClient.FormatarCodigoServico adds ".000" desdobramento automatically
                    AliquotaNfseResponse? result =
                        await _nfseApiClient.GetAliquotaAsync(
                            municipio.CodigoIbge, probeCode, competencia, cancellationToken);
                    sw.Stop();

                    _certificateProtection.OnResponseReceived(result != null ? 200 : 404, sw.Elapsed.TotalSeconds);
                    _circuitBreaker.RecordSuccess();
                    await _certificateProtection.OnItemProcessedAsync(cancellationToken);

                    if (result is not null && result.TemDados)
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
                        municipio.CodigoIbge, servico.CodigoTribNac.Replace(".", ""), competencia);

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
        _logger.LogInformation("Phase 3: Processing work queue with {Parallelism} parallel workers", _configuracao.MaxItensParalelos);

        // Track consecutive misses for early-stop per group (XX.XX.XX) — thread-safe
        System.Collections.Concurrent.ConcurrentDictionary<string, int> consecutiveMissesByGroup = new();
        SemaphoreSlim semaphore = new(_configuracao.MaxItensParalelos, _configuracao.MaxItensParalelos);

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

            IReadOnlyList<FilaProcessamento> batch = await _filaRepository.GetPendingAsync(_configuracao.TamanhoLoteMongo);

            if (batch.Count == 0)
            {
                break;
            }

            List<Task> tasks = new(batch.Count);

            foreach (FilaProcessamento item in batch)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_certificateProtection.ShouldHalt || _certificateProtection.BudgetExhausted)
                {
                    break;
                }

                // Early-stop check
                string group = ExtractGroup(item.CodigoServico);
                if (consecutiveMissesByGroup.TryGetValue(group, out int misses) && misses >= _configuracao.LimiteParadaAntecipada)
                {
                    item.MarcarConcluido();
                    await _filaRepository.UpdateStatusAsync(item);
                    execucao.IncrementarProcessados();
                    continue;
                }

                await semaphore.WaitAsync(cancellationToken);

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await ProcessarItemAsync(item, execucao, competencia, consecutiveMissesByGroup, cancellationToken);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks);
            await _execucaoRepository.UpdateAsync(execucao);
        }
    }

    internal async Task ProcessarItemAsync(
        FilaProcessamento item,
        ExecucaoCrawler execucao,
        string competencia,
        System.Collections.Concurrent.ConcurrentDictionary<string, int> consecutiveMissesByGroup,
        CancellationToken cancellationToken)
    {
        item.MarcarProcessando();
        await _filaRepository.UpdateStatusAsync(item);

        try
        {
            // Determine if this seed code needs detalhamento iteration.
            // Seed codes have format "XX.XX.00" — the "00" detalhamento is a placeholder.
            // We iterate detalhamento 01, 02, 03... and for each, desdobramento 000, 001, 002... until 404.
            bool needsIteration = NeedsDetalhamentoIteration(item.CodigoServico);

            if (needsIteration)
            {
                await ProcessarItemComIteracaoAsync(
                    item, execucao, competencia, consecutiveMissesByGroup, cancellationToken);
            }
            else
            {
                await ProcessarItemDiretoAsync(
                    item, execucao, competencia, consecutiveMissesByGroup, cancellationToken);
            }
        }
        catch (HttpRequestException ex)
        {
            int statusCode = (int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError);
            _certificateProtection.OnResponseReceived(statusCode, 0);
            _circuitBreaker.RecordFailure();
            await _certificateProtection.OnItemProcessedAsync(cancellationToken);

            bool isRetryable = statusCode >= 500 || statusCode == 0;

            if (isRetryable && item.PodeRetentar(_configuracao.MaxTentativas))
            {
                item.MarcarErro(ex.Message, _configuracao.MaxTentativas);
            }
            else
            {
                item.MarcarErro(ex.Message, 0);
                execucao.IncrementarErros(
                    $"Municipio={item.CodigoMunicipio}, Servico={item.CodigoServico}: {ex.Message}");
            }

            await _filaRepository.UpdateStatusAsync(item);
        }
        catch (TaskCanceledException)
        {
            if (item.PodeRetentar(_configuracao.MaxTentativas))
            {
                item.MarcarErro("Timeout", _configuracao.MaxTentativas);
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

    /// <summary>
    /// Processa um item que NÃO precisa de iteração de detalhamento (já tem código completo).
    /// Usado quando o código de serviço já tem detalhamento válido (ex: "01.01.01").
    /// </summary>
    internal async Task ProcessarItemDiretoAsync(
        FilaProcessamento item,
        ExecucaoCrawler execucao,
        string competencia,
        System.Collections.Concurrent.ConcurrentDictionary<string, int> consecutiveMissesByGroup,
        CancellationToken cancellationToken)
    {
        await _rateLimiter.WaitAsync(cancellationToken);
        await _circuitBreaker.WaitIfOpenAsync(cancellationToken);

        Stopwatch sw = Stopwatch.StartNew();
        AliquotaNfseResponse? result =
            await _nfseApiClient.GetAliquotaAsync(
                item.CodigoMunicipio, item.CodigoServico, competencia, cancellationToken);
        sw.Stop();

        _certificateProtection.OnResponseReceived(result != null ? 200 : 404, sw.Elapsed.TotalSeconds);
        _circuitBreaker.RecordSuccess();
        await _certificateProtection.OnItemProcessedAsync(cancellationToken);

        string group = ExtractGroup(item.CodigoServico);

        if (result is not null && result.TemDados)
        {
            consecutiveMissesByGroup[group] = 0;

            Municipio? municipio = await _municipioRepository.GetByCodigoIbgeAsync(item.CodigoMunicipio);
            string nomeMunicipio = municipio?.Nome ?? item.CodigoMunicipio;

            int aliquotasSalvas = await ExtrairESalvarAliquotasAsync(
                result, item.CodigoMunicipio, nomeMunicipio, item.CodigoServico, competencia);

            _logger.LogDebug(
                "Saved {Count} aliquotas for municipality {Municipio}, service {Servico}",
                aliquotasSalvas, item.CodigoMunicipio, item.CodigoServico);

            item.MarcarConcluido();
            await _filaRepository.UpdateStatusAsync(item);
            execucao.IncrementarProcessados();
        }
        else
        {
            consecutiveMissesByGroup.AddOrUpdate(group, 1, (_, old) => old + 1);

            item.MarcarConcluido();
            await _filaRepository.UpdateStatusAsync(item);
            execucao.IncrementarProcessados();
        }
    }

    /// <summary>
    /// Processa um item de seed que precisa de iteração de detalhamento.
    /// Seed codes têm formato "XX.XX.00" — o "00" é placeholder.
    /// Itera detalhamento 01, 02, 03... com desdobramento 000 para cada.
    /// Para quando encontra MaxConsecutiveDetalhamentoMisses 404s consecutivos.
    /// </summary>
    internal async Task ProcessarItemComIteracaoAsync(
        FilaProcessamento item,
        ExecucaoCrawler execucao,
        string competencia,
        System.Collections.Concurrent.ConcurrentDictionary<string, int> consecutiveMissesByGroup,
        CancellationToken cancellationToken)
    {
        // Extract base: "01.01.00" → item="01", subitem="01"
        (string itemPart, string subitemPart) = ExtrairItemSubitem(item.CodigoServico);

        Municipio? municipio = await _municipioRepository.GetByCodigoIbgeAsync(item.CodigoMunicipio);
        string nomeMunicipio = municipio?.Nome ?? item.CodigoMunicipio;

        int totalAliquotasSalvas = 0;
        int consecutiveDetalhamentoMisses = 0;

        for (int det = 1; det <= _configuracao.MaxDetalhamento; det++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_certificateProtection.ShouldHalt || _certificateProtection.BudgetExhausted)
            {
                break;
            }

            if (consecutiveDetalhamentoMisses >= _configuracao.MaxFalhasConsecutivasDetalhamento)
            {
                _logger.LogDebug(
                    "Stopping detalhamento iteration for {Item}.{Subitem} at {Det:D2} after {Misses} consecutive misses",
                    itemPart, subitemPart, det, consecutiveDetalhamentoMisses);
                break;
            }

            // Try desdobramento 000 first (most common)
            string codigoDetalhamento = $"{itemPart}.{subitemPart}.{det:D2}";
            string codigoCompleto = $"{codigoDetalhamento}.000";

            await _rateLimiter.WaitAsync(cancellationToken);
            await _circuitBreaker.WaitIfOpenAsync(cancellationToken);

            Stopwatch sw = Stopwatch.StartNew();
            AliquotaNfseResponse? result =
                await _nfseApiClient.GetAliquotaAsync(
                    item.CodigoMunicipio, codigoCompleto, competencia, cancellationToken);
            sw.Stop();

            _certificateProtection.OnResponseReceived(result != null ? 200 : 404, sw.Elapsed.TotalSeconds);
            _circuitBreaker.RecordSuccess();
            await _certificateProtection.OnItemProcessedAsync(cancellationToken);

            if (result is null || !result.TemDados)
            {
                consecutiveDetalhamentoMisses++;
                continue;
            }

            // Found data! Reset consecutive misses and save
            consecutiveDetalhamentoMisses = 0;

            int saved = await ExtrairESalvarAliquotasAsync(
                result, item.CodigoMunicipio, nomeMunicipio, item.CodigoServico, competencia);
            totalAliquotasSalvas += saved;

            _logger.LogDebug(
                "Found {Count} aliquotas for {Municipio} service {Codigo}",
                saved, item.CodigoMunicipio, codigoCompleto);

            // Now iterate desdobramentos 001, 002, ... for this detalhamento
            int consecutiveDesdobramentoMisses = 0;

            for (int desdobramento = 1; desdobramento <= _configuracao.MaxDesdobramento; desdobramento++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_certificateProtection.ShouldHalt || _certificateProtection.BudgetExhausted)
                {
                    break;
                }

                if (consecutiveDesdobramentoMisses >= _configuracao.MaxFalhasConsecutivasDesdobramento)
                {
                    break;
                }

                string codigoDesdobramento = $"{codigoDetalhamento}.{desdobramento:D3}";

                await _rateLimiter.WaitAsync(cancellationToken);
                await _circuitBreaker.WaitIfOpenAsync(cancellationToken);

                Stopwatch swDesdobramento = Stopwatch.StartNew();
                AliquotaNfseResponse? resultDesdobramento =
                    await _nfseApiClient.GetAliquotaAsync(
                        item.CodigoMunicipio, codigoDesdobramento, competencia, cancellationToken);
                swDesdobramento.Stop();

                _certificateProtection.OnResponseReceived(resultDesdobramento != null ? 200 : 404, swDesdobramento.Elapsed.TotalSeconds);
                _circuitBreaker.RecordSuccess();
                await _certificateProtection.OnItemProcessedAsync(cancellationToken);

                if (resultDesdobramento is null || !resultDesdobramento.TemDados)
                {
                    consecutiveDesdobramentoMisses++;
                    continue;
                }

                consecutiveDesdobramentoMisses = 0;
                int savedDesdobramento = await ExtrairESalvarAliquotasAsync(
                    resultDesdobramento, item.CodigoMunicipio, nomeMunicipio, item.CodigoServico, competencia);
                totalAliquotasSalvas += savedDesdobramento;

                _logger.LogDebug(
                    "Found {Count} aliquotas for {Municipio} service {Codigo}",
                    savedDesdobramento, item.CodigoMunicipio, codigoDesdobramento);
            }
        }

        // Update group tracking
        string group = ExtractGroup(item.CodigoServico);
        if (totalAliquotasSalvas > 0)
        {
            consecutiveMissesByGroup[group] = 0;
        }
        else
        {
            consecutiveMissesByGroup.AddOrUpdate(group, 1, (_, old) => old + 1);
        }

        _logger.LogDebug(
            "Detalhamento iteration for {Municipio}/{CodigoServico}: saved {Total} aliquotas",
            item.CodigoMunicipio, item.CodigoServico, totalAliquotasSalvas);

        item.MarcarConcluido();
        await _filaRepository.UpdateStatusAsync(item);
        execucao.IncrementarProcessados();
    }

    /// <summary>
    /// Determina se o código de serviço precisa de iteração de detalhamento.
    /// Códigos com detalhamento "00" (seed placeholder) precisam de iteração.
    /// Exemplo: "01.01.00" → true, "01.01.01" → false
    /// </summary>
    internal static bool NeedsDetalhamentoIteration(string codigoServico)
    {
        string clean = codigoServico.Replace(".", "");

        // Deve ter pelo menos 6 dígitos: item(2) + subitem(2) + detalhamento(2)
        if (clean.Length < 6) return false;

        // Se o detalhamento (posições 4-5) for "00", precisa de iteração
        string detalhamento = clean[4..6];
        return detalhamento == "00";
    }

    /// <summary>
    /// Extrai item e subitem do código de serviço.
    /// "01.01.00" → ("01", "01")
    /// "010100" → ("01", "01")
    /// </summary>
    internal static (string Item, string Subitem) ExtrairItemSubitem(string codigoServico)
    {
        string clean = codigoServico.Replace(".", "");
        return (clean[..2], clean[2..4]);
    }

    /// <summary>
    /// Extrai todas as alíquotas da resposta da API e persiste.
    /// A resposta contém um dicionário onde a chave é o código do serviço (ex: "01.01.01.000")
    /// e o valor é uma lista de alíquotas com Incidencia, Aliq, DtIni, DtFim.
    /// Salva apenas alíquotas vigentes (DtFim null ou futura).
    /// </summary>
    internal async Task<int> ExtrairESalvarAliquotasAsync(
        AliquotaNfseResponse response,
        string codigoMunicipio,
        string nomeMunicipio,
        string codigoServicoBase,
        string competencia)
    {
        if (response.Aliquotas is null || response.Aliquotas.Count == 0)
        {
            return 0;
        }

        // Buscar descrição do serviço na tabela de serviços
        string descricaoServico = string.Empty;
        var servico = await _servicoRepository.GetByCodigoAsync(codigoServicoBase);
        if (servico is not null)
        {
            descricaoServico = servico.Descricao;
        }

        int count = 0;

        foreach (KeyValuePair<string, List<AliquotaItem>> entry in response.Aliquotas)
        {
            string codigoServicoApi = entry.Key; // e.g., "01.01.01.000"

            foreach (AliquotaItem aliquotaItem in entry.Value)
            {
                // Only save current (vigente) aliquotas with a valid Aliq value
                if (!aliquotaItem.Vigente || !aliquotaItem.Aliq.HasValue)
                {
                    continue;
                }

                string codigoServicoNormalizado = codigoServicoBase.Replace(".", "");
                Aliquota aliquota = Aliquota.Create(
                    codigoMunicipio,
                    nomeMunicipio,
                    codigoServicoNormalizado,
                    FormatServiceCode(codigoServicoApi),
                    descricaoServico,
                    aliquotaItem.Aliq.Value,
                    competencia,
                    "API NFS-e Nacional");

                await _aliquotaRepository.UpsertAsync(aliquota);
                count++;
            }
        }

        return count;
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

    private void ValidarConfiguracao()
    {
        ConfiguracaoCrawler padrao = ConfiguracaoCrawler.CriarPadrao();

        if (_configuracao.MaxItensParalelos <= 0)
        {
            _logger.LogWarning("MaxItensParalelos inválido ({Valor}), usando padrão ({Padrao})",
                _configuracao.MaxItensParalelos, padrao.MaxItensParalelos);
            _configuracao.AtualizarParcial(maxItensParalelos: padrao.MaxItensParalelos);
        }

        if (_configuracao.TamanhoLoteMongo <= 0)
        {
            _logger.LogWarning("TamanhoLoteMongo inválido ({Valor}), usando padrão ({Padrao})",
                _configuracao.TamanhoLoteMongo, padrao.TamanhoLoteMongo);
            _configuracao.AtualizarParcial(tamanhoLoteMongo: padrao.TamanhoLoteMongo);
        }

        if (_configuracao.TamanhoLoteCertificado <= 0)
        {
            _logger.LogWarning("TamanhoLoteCertificado inválido ({Valor}), usando padrão ({Padrao})",
                _configuracao.TamanhoLoteCertificado, padrao.TamanhoLoteCertificado);
            _configuracao.AtualizarParcial(tamanhoLoteCertificado: padrao.TamanhoLoteCertificado);
        }

        if (_configuracao.MaxTentativas <= 0)
        {
            _logger.LogWarning("MaxTentativas inválido ({Valor}), usando padrão ({Padrao})",
                _configuracao.MaxTentativas, padrao.MaxTentativas);
            _configuracao.AtualizarParcial(maxTentativas: padrao.MaxTentativas);
        }
    }
}
