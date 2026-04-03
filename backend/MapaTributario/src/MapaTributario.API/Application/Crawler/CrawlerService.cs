using System.Collections.Concurrent;
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

    // Serializa chamadas concorrentes a _execucaoRepository.UpdateAsync(execucao)
    // para evitar que ReplaceOneAsync (full-document replace) de UFs paralelas sobrescrevam dados umas das outras.
    private readonly SemaphoreSlim _semaforoPersistencia = new(1, 1);

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
        bool? filtroCapital = null,
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
            execucao.AvancarFase(FaseCrawler.DescobertaConvenios);
            await _execucaoRepository.UpdateAsync(execucao);
            List<Municipio> municipiosAtivos = await FaseConvenioAsync(execucao, filtroUfs, filtroCapital, cancellationToken);

            if (municipiosAtivos.Count == 0)
            {
                _logger.LogWarning("No active municipalities found. Ending execution");
                StatusExecucao statusSaida = execucao.Erros > 0
                    ? StatusExecucao.FalhaParcial
                    : StatusExecucao.Concluido;
                execucao.Finalizar(statusSaida);
                await _execucaoRepository.UpdateAsync(execucao);
                return Result.Ok(execucao);
            }

            // Phase 2: Probe municipalities
            execucao.AvancarFase(FaseCrawler.Sondagem);
            await _execucaoRepository.UpdateAsync(execucao);
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
            execucao.AvancarFase(FaseCrawler.ProcessamentoFila);
            await _execucaoRepository.UpdateAsync(execucao);
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
        bool? filtroCapital,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Phase 1: Discovering municipalities via convenio endpoint");

        ConcurrentBag<Municipio> todosAtivos = new();

        IReadOnlyList<string> ufsParaProcessar = filtroUfs is { Count: > 0 }
            ? filtroUfs.Select(u => u.ToUpperInvariant()).Where(u => UfsBrasil.Todas.Contains(u)).ToList()
            : UfsBrasil.Todas.ToList();

        if (filtroUfs is { Count: > 0 })
        {
            _logger.LogInformation("Filtering execution to UFs: {Ufs}", string.Join(", ", ufsParaProcessar));
        }

        // CancellationTokenSource derivado para propagar interrupção de proteção de certificado
        using CancellationTokenSource ctsProtecao = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        ParallelOptions opcoes = new()
        {
            MaxDegreeOfParallelism = _configuracao.MaxUfsParalelas,
            CancellationToken = ctsProtecao.Token
        };

        try
        {
            await Parallel.ForEachAsync(ufsParaProcessar, opcoes, async (uf, tokenParalelo) =>
            {
                await ProcessarUfConvenioAsync(execucao, uf, filtroCapital, todosAtivos, ctsProtecao, tokenParalelo);
            });
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Interrupção veio da proteção de certificado, não do caller — tratamento gracioso
            _logger.LogWarning("Phase 1 interrupted by certificate protection halt");
        }

        _logger.LogInformation("Phase 1 complete. {Active} active municipalities found across all UFs",
            todosAtivos.Count);

        return todosAtivos.ToList();
    }

    internal async Task ProcessarUfConvenioAsync(
        ExecucaoCrawler execucao,
        string uf,
        bool? filtroCapital,
        ConcurrentBag<Municipio> todosAtivos,
        CancellationTokenSource ctsProtecao,
        CancellationToken cancellationToken)
    {
        execucao.IniciarProcessamentoUf(uf);
        await _execucaoRepository.UpdateAsync(execucao);

        List<Municipio> ativosUf = new();
        int municipiosEncontrados = 0;
        int errosUf = 0;
        int verificadosUf = 0;
        bool ufInterrompida = false;

        try
        {
            IReadOnlyList<Municipio> porUf = await _municipioRepository.GetByUfAsync(uf);
            municipiosEncontrados = porUf.Count;

            // Filtrar por capital quando solicitado
            List<Municipio> municipiosParaVerificar = porUf.ToList();
            if (filtroCapital.HasValue)
            {
                municipiosParaVerificar = municipiosParaVerificar
                    .Where(m => m.EhCapital == filtroCapital.Value).ToList();
            }

            // Priorizar capitais dentro da UF
            municipiosParaVerificar = municipiosParaVerificar
                .OrderByDescending(m => m.EhCapital)
                .ThenBy(m => m.Nome)
                .ToList();

            foreach (Municipio municipio in municipiosParaVerificar)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_certificateProtection.ShouldHalt || _certificateProtection.BudgetExhausted)
                {
                    ufInterrompida = true;
                    // Sinalizar proteção para todas as UFs paralelas
                    try { ctsProtecao.Cancel(); } catch (ObjectDisposedException) { }
                    break;
                }

                verificadosUf++;

                try
                {
                    await Task.WhenAll(
                        _rateLimiter.WaitAsync(cancellationToken),
                        _circuitBreaker.WaitIfOpenAsync(cancellationToken));

                    Stopwatch sw = Stopwatch.StartNew();
                    ConvenioNfseResponse? convenio =
                        await _nfseApiClient.GetConvenioAsync(municipio.CodigoIbge, cancellationToken);
                    sw.Stop();

                    _certificateProtection.OnResponseReceived(200, sw.Elapsed.TotalSeconds);
                    _circuitBreaker.RecordSuccess();
                    await _certificateProtection.OnItemProcessedAsync(cancellationToken);

                    if (convenio is not null && convenio.Ativo)
                    {
                        ativosUf.Add(municipio);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Cancelamento durante operação de I/O — tratar como interrupção
                    ufInterrompida = true;
                    break;
                }
                catch (HttpRequestException ex)
                {
                    int statusCode = (int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError);
                    _certificateProtection.OnResponseReceived(statusCode, 0);
                    _circuitBreaker.RecordFailure();
                    await _certificateProtection.OnItemProcessedAsync(cancellationToken);
                    errosUf++;

                    _logger.LogWarning(
                        "Failed to check convenio for municipality {CodigoIbge}: {Message}",
                        municipio.CodigoIbge, ex.Message);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelamento propagado pelo CTS derivado (outra UF disparou proteção)
            ufInterrompida = true;
        }
        catch (Exception ex)
        {
            // Exceção inesperada — marcar UF como falha para não ficar presa como "EmAndamento"
            _logger.LogError(ex, "Phase 1 [{Uf}]: Unexpected error during convenio discovery", uf);
            execucao.IncrementarErros($"Fase 1 [{uf}]: {ex.Message}");
            execucao.FalharProcessamentoUf(uf, municipiosEncontrados);
            await _execucaoRepository.UpdateAsync(execucao);

            foreach (Municipio ativo in ativosUf)
            {
                todosAtivos.Add(ativo);
            }

            return;
        }

        // Determinar status da UF com base no resultado real — SEMPRE executado
        if (ufInterrompida)
        {
            execucao.InterromperProcessamentoUf(uf, municipiosEncontrados, ativosUf.Count);
        }
        else if (verificadosUf > 0 && errosUf == verificadosUf)
        {
            execucao.FalharProcessamentoUf(uf, municipiosEncontrados);
        }
        else
        {
            execucao.FinalizarProcessamentoUf(uf, municipiosEncontrados, ativosUf.Count);
        }

        // Persistir progresso da UF no MongoDB para observabilidade em tempo real
        await _execucaoRepository.UpdateAsync(execucao);

        foreach (Municipio ativo in ativosUf)
        {
            todosAtivos.Add(ativo);
        }
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
                    await Task.WhenAll(
                        _rateLimiter.WaitAsync(cancellationToken),
                        _circuitBreaker.WaitIfOpenAsync(cancellationToken));

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
                    execucaoId,
                    municipio.SiglaEstado));
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
        _logger.LogInformation(
            "Phase 3: Processing work queue — {MaxUfs} UFs in parallel, {MaxItens} workers per UF",
            _configuracao.MaxUfsParalelas,
            _configuracao.MaxItensParalelos);

        IReadOnlyList<string> ufsComPendencia = await _filaRepository.GetDistinctPendingUfsAsync();

        // Fallback: processar itens legados (sem campo Uf preenchido) via GetPendingAsync sequencial.
        // Itens criados antes desta PR podem não ter Uf, ficando invisíveis para GetDistinctPendingUfsAsync.
        await ProcessarItensLegadosSemUfAsync(execucao, competencia, cancellationToken);

        if (ufsComPendencia.Count == 0)
        {
            _logger.LogInformation("Phase 3: No pending items in queue");
            return;
        }

        _logger.LogInformation("Phase 3: Found {Count} UFs with pending items: {Ufs}",
            ufsComPendencia.Count, string.Join(", ", ufsComPendencia));

        // Track consecutive misses for early-stop per group (XX.XX.XX) — thread-safe, shared across UFs
        ConcurrentDictionary<string, int> consecutiveMissesByGroup = new();

        using CancellationTokenSource ctsProtecao = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        ParallelOptions opcoes = new()
        {
            MaxDegreeOfParallelism = _configuracao.MaxUfsParalelas,
            CancellationToken = ctsProtecao.Token
        };

        try
        {
            await Parallel.ForEachAsync(ufsComPendencia, opcoes, async (uf, tokenParalelo) =>
            {
                await ProcessarFilaUfAsync(execucao, uf, competencia, consecutiveMissesByGroup, ctsProtecao, tokenParalelo);
            });
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Phase 3 interrupted by certificate protection halt");
        }
    }

    /// <summary>
    /// Processa itens legados na fila que não possuem o campo Uf preenchido.
    /// Estes itens foram criados antes da PR de multi-UF e são invisíveis para
    /// GetDistinctPendingUfsAsync/GetPendingByUfAsync. Processa sequencialmente
    /// via GetPendingAsync até esgotar.
    /// </summary>
    internal async Task ProcessarItensLegadosSemUfAsync(
        ExecucaoCrawler execucao,
        string competencia,
        CancellationToken cancellationToken)
    {
        ConcurrentDictionary<string, int> misses = new();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_certificateProtection.ShouldHalt || _certificateProtection.BudgetExhausted)
            {
                break;
            }

            IReadOnlyList<FilaProcessamento> batch = await _filaRepository.GetPendingAsync(_configuracao.TamanhoLoteMongo);

            // Filtrar apenas itens sem Uf (legados) — itens com Uf serão processados pelo loop principal
            List<FilaProcessamento> itensLegados = batch.Where(i => string.IsNullOrEmpty(i.Uf)).ToList();

            if (itensLegados.Count == 0)
            {
                break;
            }

            _logger.LogInformation(
                "Phase 3 [legacy]: Processing {Count} legacy items without Uf field",
                itensLegados.Count);

            foreach (FilaProcessamento item in itensLegados)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_certificateProtection.ShouldHalt || _certificateProtection.BudgetExhausted)
                {
                    break;
                }

                try
                {
                    await ProcessarItemAsync(item, execucao, competencia, misses, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Phase 3 [legacy]: Error processing item {Id}", item.Id);
                }
            }

            await PersistirExecucaoAsync(execucao);
        }
    }

    internal async Task ProcessarFilaUfAsync(
        ExecucaoCrawler execucao,
        string uf,
        string competencia,
        ConcurrentDictionary<string, int> consecutiveMissesByGroup,
        CancellationTokenSource ctsProtecao,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Phase 3 [{Uf}]: Starting queue processing", uf);

        // Usar métodos dedicados da Fase 3 para NÃO sobrescrever ProgressoUfs da Fase 1
        execucao.IniciarProcessamentoFilaUf(uf);
        await PersistirExecucaoAsync(execucao);

        int processadosUf = 0;
        int errosUf = 0;
        SemaphoreSlim semaphore = new(_configuracao.MaxItensParalelos, _configuracao.MaxItensParalelos);

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_certificateProtection.ShouldHalt)
                {
                    _logger.LogCritical("Phase 3 [{Uf}]: Certificate protection halt triggered", uf);
                    await ctsProtecao.CancelAsync();
                    break;
                }

                if (_certificateProtection.BudgetExhausted)
                {
                    _logger.LogWarning("Phase 3 [{Uf}]: Daily budget exhausted", uf);
                    await ctsProtecao.CancelAsync();
                    break;
                }

                IReadOnlyList<FilaProcessamento> batch = await _filaRepository.GetPendingByUfAsync(uf, _configuracao.TamanhoLoteMongo);

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
                        Interlocked.Increment(ref processadosUf);
                        continue;
                    }

                    await semaphore.WaitAsync(cancellationToken);

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await ProcessarItemAsync(item, execucao, competencia, consecutiveMissesByGroup, cancellationToken);
                            Interlocked.Increment(ref processadosUf);
                        }
                        catch (Exception)
                        {
                            Interlocked.Increment(ref errosUf);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, cancellationToken));
                }

                await Task.WhenAll(tasks);
                await PersistirExecucaoAsync(execucao);
            }

            execucao.FinalizarProcessamentoFilaUf(uf);
            _logger.LogInformation(
                "Phase 3 [{Uf}]: Completed — {Processados} processed, {Erros} errors",
                uf, processadosUf, errosUf);
        }
        catch (OperationCanceledException)
        {
            execucao.FinalizarProcessamentoFilaUf(uf);
            _logger.LogWarning("Phase 3 [{Uf}]: Interrupted", uf);
        }
        catch (Exception ex)
        {
            execucao.FinalizarProcessamentoFilaUf(uf);
            _logger.LogError(ex, "Phase 3 [{Uf}]: Failed with unexpected error", uf);
        }
        finally
        {
            semaphore.Dispose();
            await PersistirExecucaoAsync(execucao);
        }
    }

    internal async Task ProcessarItemAsync(
        FilaProcessamento item,
        ExecucaoCrawler execucao,
        string competencia,
        ConcurrentDictionary<string, int> consecutiveMissesByGroup,
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
        ConcurrentDictionary<string, int> consecutiveMissesByGroup,
        CancellationToken cancellationToken)
    {
                    await Task.WhenAll(
                        _rateLimiter.WaitAsync(cancellationToken),
                        _circuitBreaker.WaitIfOpenAsync(cancellationToken));

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
        ConcurrentDictionary<string, int> consecutiveMissesByGroup,
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

            await Task.WhenAll(
                _rateLimiter.WaitAsync(cancellationToken),
                _circuitBreaker.WaitIfOpenAsync(cancellationToken));

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

                await Task.WhenAll(
                    _rateLimiter.WaitAsync(cancellationToken),
                    _circuitBreaker.WaitIfOpenAsync(cancellationToken));

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

        if (_configuracao.MaxUfsParalelas <= 0)
        {
            _logger.LogWarning("MaxUfsParalelas inválido ({Valor}), usando padrão ({Padrao})",
                _configuracao.MaxUfsParalelas, padrao.MaxUfsParalelas);
            _configuracao.AtualizarParcial(maxUfsParalelas: padrao.MaxUfsParalelas);
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

    /// <summary>
    /// Persiste o estado da execução no MongoDB de forma thread-safe.
    /// Usa SemaphoreSlim para serializar chamadas concorrentes e evitar que
    /// ReplaceOneAsync (full-document replace) de UFs paralelas sobrescrevam dados.
    /// </summary>
    internal async Task PersistirExecucaoAsync(ExecucaoCrawler execucao)
    {
        await _semaforoPersistencia.WaitAsync();
        try
        {
            await _execucaoRepository.UpdateAsync(execucao);
        }
        finally
        {
            _semaforoPersistencia.Release();
        }
    }
}
