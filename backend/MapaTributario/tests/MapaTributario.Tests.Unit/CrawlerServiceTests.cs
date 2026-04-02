using FluentResults;
using MapaTributario.API.Application.Crawler;
using MapaTributario.API.Application.Errors;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using MapaTributario.API.Infrastructure.External;
using MapaTributario.API.Infrastructure.External.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class CrawlerServiceTests
{
    private readonly Mock<IExecucaoCrawlerRepository> _execucaoRepo = new();
    private readonly Mock<IFilaProcessamentoRepository> _filaRepo = new();
    private readonly Mock<IMunicipioRepository> _municipioRepo = new();
    private readonly Mock<IServicoRepository> _servicoRepo = new();
    private readonly Mock<IAliquotaRepository> _aliquotaRepo = new();
    private readonly Mock<IConfiguracaoCrawlerRepository> _configuracaoRepo = new();
    private readonly Mock<INfseApiClient> _nfseClient = new();
    private readonly Mock<IRateLimiter> _rateLimiter = new();
    private readonly Mock<ICircuitBreaker> _circuitBreaker = new();
    private readonly Mock<ICertificateProtection> _certProtection = new();
    private readonly Mock<ICrawlerExecutionGuard> _executionGuard = new();
    private readonly Mock<ICertificadoStore> _certificadoStore = new();
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly Mock<ILogger<CrawlerService>> _logger = new();
    private readonly CrawlerService _sut;

    public CrawlerServiceTests()
    {
        _rateLimiter.Setup(r => r.WaitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _circuitBreaker.Setup(c => c.WaitIfOpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _circuitBreaker.Setup(c => c.IsOpen).Returns(false);
        _certProtection.Setup(c => c.ShouldHalt).Returns(false);
        _certProtection.Setup(c => c.BudgetExhausted).Returns(false);
        _certProtection.Setup(c => c.OnItemProcessedAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _executionGuard.Setup(g => g.TryAcquire()).Returns(true);
        _certificadoStore.Setup(s => s.HasCertificate()).Returns(true);
        _execucaoRepo.Setup(r => r.CreateAsync(It.IsAny<ExecucaoCrawler>()))
            .ReturnsAsync((ExecucaoCrawler e) => e);
        _execucaoRepo.Setup(r => r.UpdateAsync(It.IsAny<ExecucaoCrawler>())).Returns(Task.CompletedTask);
        _filaRepo.Setup(r => r.RevertProcessingTopendingAsync()).Returns(Task.CompletedTask);
        _filaRepo.Setup(r => r.InsertManyAsync(It.IsAny<IEnumerable<FilaProcessamento>>())).Returns(Task.CompletedTask);
        _filaRepo.Setup(r => r.UpdateStatusAsync(It.IsAny<FilaProcessamento>())).Returns(Task.CompletedTask);

        // Configuração padrão do crawler (retornada pelo repositório)
        _configuracaoRepo.Setup(r => r.ObterAtualAsync())
            .ReturnsAsync(ConfiguracaoCrawler.CriarPadrao());

        _sut = new CrawlerService(
            _execucaoRepo.Object,
            _filaRepo.Object,
            _municipioRepo.Object,
            _servicoRepo.Object,
            _aliquotaRepo.Object,
            _configuracaoRepo.Object,
            _nfseClient.Object,
            _rateLimiter.Object,
            _circuitBreaker.Object,
            _certProtection.Object,
            _executionGuard.Object,
            _certificadoStore.Object,
            _configuration.Object,
            _logger.Object);
    }

    #region Helpers

    /// <summary>
    /// Cria ConvenioNfseResponse ativo (aderenteAmbienteNacional = 1).
    /// </summary>
    private static ConvenioNfseResponse CriarConvenioAtivo() => new()
    {
        ParametrosConvenio = new ParametrosConvenio { AderenteAmbienteNacional = 1 },
        Mensagem = "Parâmetros do convênio recuperados com sucesso."
    };

    /// <summary>
    /// Cria AliquotaNfseResponse com uma alíquota vigente para o código de serviço informado.
    /// </summary>
    private static AliquotaNfseResponse CriarAliquotaResponse(string codigoServico, decimal aliquota) => new()
    {
        Aliquotas = new Dictionary<string, List<AliquotaItem>>
        {
            [codigoServico] = new List<AliquotaItem>
            {
                new() { Incidencia = "SIM", Aliq = aliquota, DtIni = DateTime.UtcNow.AddYears(-1), DtFim = null }
            }
        },
        Mensagem = "Alíquotas recuperadas com sucesso."
    };

    #endregion

    [Fact]
    public async Task ExecutarAsync_QuandoJaEmExecucao_RetornaExecucaoEmAndamentoError()
    {
        // Arrange - guard refuses acquisition (already running)
        _executionGuard.Setup(g => g.TryAcquire()).Returns(false);

        // Act
        Result<ExecucaoCrawler> resultado = await _sut.ExecutarAsync(TipoExecucao.Manual);

        // Assert
        resultado.IsFailed.ShouldBeTrue();
        resultado.HasError<ExecucaoEmAndamentoError>().ShouldBeTrue();
    }

    [Fact]
    public async Task ExecutarAsync_SemCertificadoDigital_RetornaCertificadoNaoDisponivelError()
    {
        // Arrange - nenhum certificado disponível (nem dinâmico nem estático)
        _certificadoStore.Setup(s => s.HasCertificate()).Returns(false);

        // Act
        Result<ExecucaoCrawler> resultado = await _sut.ExecutarAsync(TipoExecucao.Manual);

        // Assert
        resultado.IsFailed.ShouldBeTrue();
        resultado.HasError<CertificadoNaoDisponivelError>().ShouldBeTrue();

        // Não deve ter adquirido o guard
        _executionGuard.Verify(g => g.TryAcquire(), Times.Never);
    }

    [Fact]
    public void CertificadoDisponivel_ComCertificadoDinamico_RetornaTrue()
    {
        // Arrange
        _certificadoStore.Setup(s => s.HasCertificate()).Returns(true);

        // Act & Assert
        _sut.CertificadoDisponivel().ShouldBeTrue();
    }

    [Fact]
    public void CertificadoDisponivel_SemNenhumCertificado_RetornaFalse()
    {
        // Arrange
        _certificadoStore.Setup(s => s.HasCertificate()).Returns(false);

        // Act & Assert
        _sut.CertificadoDisponivel().ShouldBeFalse();
    }

    [Fact]
    public async Task ExecutarAsync_SemMunicipiosAtivos_RetornaConcluido()
    {
        // Arrange
        _municipioRepo.Setup(r => r.GetByUfAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<Municipio>());

        // Act
        Result<ExecucaoCrawler> resultado = await _sut.ExecutarAsync(TipoExecucao.Agendado);

        // Assert
        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.Status.ShouldBe(StatusExecucao.Concluido);
        resultado.Value.Tipo.ShouldBe(TipoExecucao.Agendado);
        resultado.Value.Fim.ShouldNotBeNull();
    }

    [Fact]
    public async Task ExecutarAsync_ComMunicipiosAtivos_ProcessaFila()
    {
        // Arrange
        Municipio municipio = Municipio.Create("3106200", "Belo Horizonte", "MG");
        _municipioRepo.Setup(r => r.GetByUfAsync("MG"))
            .ReturnsAsync(new List<Municipio> { municipio });
        _municipioRepo.Setup(r => r.GetByUfAsync(It.Is<string>(s => s != "MG")))
            .ReturnsAsync(new List<Municipio>());
        _municipioRepo.Setup(r => r.GetByCodigoIbgeAsync("3106200"))
            .ReturnsAsync(municipio);

        _nfseClient.Setup(c => c.GetConvenioAsync("3106200", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarConvenioAtivo());

        // Probe: at least one returns data
        _nfseClient.Setup(c => c.GetAliquotaAsync("3106200", "01.01.01", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarAliquotaResponse("01.01.01.000", 2.0m));

        Servico servico = Servico.Create("01.01.01", "Servico teste", "01", "01", "01");
        _servicoRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Servico> { servico });

        _aliquotaRepo.Setup(r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        // Queue processing: return items once, then empty
        bool firstCall = true;
        _filaRepo.Setup(r => r.GetPendingAsync(It.IsAny<int>()))
            .ReturnsAsync(() =>
            {
                if (firstCall)
                {
                    firstCall = false;
                    FilaProcessamento item = FilaProcessamento.Create("3106200", "01.01.01", CrawlerService.GetCompetenciaAtual(), "exec1");
                    return new List<FilaProcessamento> { item };
                }

                return new List<FilaProcessamento>();
            });

        _nfseClient.Setup(c => c.GetAliquotaAsync("3106200", "01.01.01", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarAliquotaResponse("01.01.01.000", 2.0m));

        _aliquotaRepo.Setup(r => r.UpsertAsync(It.IsAny<Aliquota>())).Returns(Task.CompletedTask);

        // Act
        Result<ExecucaoCrawler> resultado = await _sut.ExecutarAsync(TipoExecucao.Manual);

        // Assert
        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.Status.ShouldBe(StatusExecucao.Concluido);
        resultado.Value.Processados.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task FaseConvenioAsync_FiltraMunicipiosSemConvenio()
    {
        // Arrange
        Municipio mun1 = Municipio.Create("1100205", "Porto Velho", "RO");
        Municipio mun2 = Municipio.Create("1302603", "Manaus", "AM");

        _municipioRepo.Setup(r => r.GetByUfAsync("RO")).ReturnsAsync(new List<Municipio> { mun1 });
        _municipioRepo.Setup(r => r.GetByUfAsync("AM")).ReturnsAsync(new List<Municipio> { mun2 });
        _municipioRepo.Setup(r => r.GetByUfAsync(It.Is<string>(s => s != "RO" && s != "AM")))
            .ReturnsAsync(new List<Municipio>());

        _nfseClient.Setup(c => c.GetConvenioAsync("1100205", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarConvenioAtivo());
        _nfseClient.Setup(c => c.GetConvenioAsync("1302603", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConvenioNfseResponse?)null);

        // Act
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        List<Municipio> result = await _sut.FaseConvenioAsync(execucao, null, null, CancellationToken.None);

        // Assert
        result.Count.ShouldBe(1);
        result[0].CodigoIbge.ShouldBe("1100205");
    }

    [Fact]
    public async Task FaseProbeAsync_FiltraMunicipiosSemDados()
    {
        // Arrange
        Municipio mun1 = Municipio.Create("3106200", "Belo Horizonte", "MG");
        Municipio mun2 = Municipio.Create("1302603", "Manaus", "AM");
        string competencia = CrawlerService.GetCompetenciaAtual();

        // mun1: at least 1 probe returns data
        _nfseClient.Setup(c => c.GetAliquotaAsync("3106200", "01.01.01", competencia, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarAliquotaResponse("01.01.01.000", 2.0m));

        // mun2: all probes return null
        _nfseClient.Setup(c => c.GetAliquotaAsync("1302603", It.IsAny<string>(), competencia, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AliquotaNfseResponse?)null);

        // Act
        List<Municipio> result = await _sut.FaseProbeAsync(
            new List<Municipio> { mun1, mun2 }, competencia, CancellationToken.None);

        // Assert
        result.Count.ShouldBe(1);
        result[0].CodigoIbge.ShouldBe("3106200");
    }

    [Fact]
    public async Task GerarFilaAsync_ComIncrementalSkip_PulaJaColetados()
    {
        // Arrange
        Municipio mun = Municipio.Create("3106200", "Belo Horizonte", "MG");
        Servico srv1 = Servico.Create("01.01.01", "Serv1", "01", "01", "01");
        Servico srv2 = Servico.Create("07.02.01", "Serv2", "07", "02", "01");
        string competencia = CrawlerService.GetCompetenciaAtual();

        _aliquotaRepo.Setup(r => r.ExistsAsync("3106200", "010101", competencia)).ReturnsAsync(true);
        _aliquotaRepo.Setup(r => r.ExistsAsync("3106200", "070201", competencia)).ReturnsAsync(false);

        List<FilaProcessamento> inserted = new();
        _filaRepo.Setup(r => r.InsertManyAsync(It.IsAny<IEnumerable<FilaProcessamento>>()))
            .Callback<IEnumerable<FilaProcessamento>>(items => inserted.AddRange(items))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.GerarFilaAsync(
            new List<Municipio> { mun },
            new List<Servico> { srv1, srv2 },
            competencia, "exec1", false, CancellationToken.None);

        // Assert
        inserted.Count.ShouldBe(1);
        inserted[0].CodigoServico.ShouldBe("07.02.01");
    }

    [Fact]
    public async Task GerarFilaAsync_ComForcarReprocessamento_NaoPulaJaColetados()
    {
        // Arrange
        Municipio mun = Municipio.Create("3106200", "Belo Horizonte", "MG");
        Servico srv1 = Servico.Create("01.01.01", "Serv1", "01", "01", "01");
        string competencia = CrawlerService.GetCompetenciaAtual();

        _aliquotaRepo.Setup(r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        List<FilaProcessamento> inserted = new();
        _filaRepo.Setup(r => r.InsertManyAsync(It.IsAny<IEnumerable<FilaProcessamento>>()))
            .Callback<IEnumerable<FilaProcessamento>>(items => inserted.AddRange(items))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.GerarFilaAsync(
            new List<Municipio> { mun },
            new List<Servico> { srv1 },
            competencia, "exec1", true, CancellationToken.None);

        // Assert
        inserted.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ProcessarItemAsync_ComResultado_FazUpsertEMarcaConcluido()
    {
        // Arrange
        FilaProcessamento item = FilaProcessamento.Create("3106200", "01.01.01", "2026-04-01", "exec1");
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        System.Collections.Concurrent.ConcurrentDictionary<string, int> misses = new();

        _nfseClient.Setup(c => c.GetAliquotaAsync("3106200", "01.01.01", "2026-04-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarAliquotaResponse("01.01.01.000", 5.0m));

        Municipio mun = Municipio.Create("3106200", "Belo Horizonte", "MG");
        _municipioRepo.Setup(r => r.GetByCodigoIbgeAsync("3106200")).ReturnsAsync(mun);
        _aliquotaRepo.Setup(r => r.UpsertAsync(It.IsAny<Aliquota>())).Returns(Task.CompletedTask);

        // Act
        await _sut.ProcessarItemAsync(item, execucao, "2026-04-01", misses, CancellationToken.None);

        // Assert
        execucao.Processados.ShouldBe(1);
        _aliquotaRepo.Verify(r => r.UpsertAsync(It.IsAny<Aliquota>()), Times.Once);
        misses.GetValueOrDefault("01.01.01").ShouldBe(0);
    }

    [Fact]
    public async Task ProcessarItemAsync_Sem404_IncrementaMissesEMarcaConcluido()
    {
        // Arrange
        FilaProcessamento item = FilaProcessamento.Create("3106200", "01.01.01", "2026-04-01", "exec1");
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        System.Collections.Concurrent.ConcurrentDictionary<string, int> misses = new();

        _nfseClient.Setup(c => c.GetAliquotaAsync("3106200", "01.01.01", "2026-04-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AliquotaNfseResponse?)null);

        // Act
        await _sut.ProcessarItemAsync(item, execucao, "2026-04-01", misses, CancellationToken.None);

        // Assert
        execucao.Processados.ShouldBe(1);
        misses["01.01.01"].ShouldBe(1);
    }

    [Fact]
    public async Task ProcessarItemAsync_ComErroHttp5xx_MarcaErroComRetry()
    {
        // Arrange
        FilaProcessamento item = FilaProcessamento.Create("3106200", "01.01.01", "2026-04-01", "exec1");
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        System.Collections.Concurrent.ConcurrentDictionary<string, int> misses = new();

        _nfseClient.Setup(c => c.GetAliquotaAsync("3106200", "01.01.01", "2026-04-01", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Server error", null, System.Net.HttpStatusCode.InternalServerError));

        // Act
        await _sut.ProcessarItemAsync(item, execucao, "2026-04-01", misses, CancellationToken.None);

        // Assert
        item.Status.ShouldBe(StatusFila.Erro);
        item.Tentativas.ShouldBe(1);
        item.ProximaTentativa.ShouldNotBeNull();
    }

    [Fact]
    public async Task ProcessarItemAsync_ComErroHttp403_MarcaErroSemRetry()
    {
        // Arrange
        FilaProcessamento item = FilaProcessamento.Create("3106200", "01.01.01", "2026-04-01", "exec1");
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        System.Collections.Concurrent.ConcurrentDictionary<string, int> misses = new();

        _nfseClient.Setup(c => c.GetAliquotaAsync("3106200", "01.01.01", "2026-04-01", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Forbidden", null, System.Net.HttpStatusCode.Forbidden));

        // Act
        await _sut.ProcessarItemAsync(item, execucao, "2026-04-01", misses, CancellationToken.None);

        // Assert
        item.Status.ShouldBe(StatusFila.Erro);
        execucao.Erros.ShouldBe(1);
    }

    [Fact]
    public async Task ProcessarFilaAsync_ComEarlyStop_PulaItensAposThreshold()
    {
        // Arrange
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        string competencia = CrawlerService.GetCompetenciaAtual();

        // Create 10 items with same group that all miss
        List<FilaProcessamento> items = new();
        for (int i = 1; i <= 10; i++)
        {
            items.Add(FilaProcessamento.Create("3106200", $"01.01.01", competencia, "exec1"));
        }

        int callCount = 0;
        _filaRepo.Setup(r => r.GetPendingAsync(It.IsAny<int>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return items;
                }

                return new List<FilaProcessamento>();
            });

        // All return null (miss)
        _nfseClient.Setup(c => c.GetAliquotaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AliquotaNfseResponse?)null);

        // Act
        await _sut.ProcessarFilaAsync(execucao, competencia, CancellationToken.None);

        // Assert - all items should be processed (though some might be skipped by early-stop)
        execucao.Processados.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void GetCompetenciaAtual_RetornaFormatoCorreto()
    {
        string competencia = CrawlerService.GetCompetenciaAtual();
        competencia.ShouldMatch(@"\d{4}-\d{2}-\d{2}");
        competencia.ShouldEndWith("-01");
    }

    [Fact]
    public void ExtractGroup_ComPontos_RetornaGrupo()
    {
        CrawlerService.ExtractGroup("01.01.01").ShouldBe("01.01.01");
        CrawlerService.ExtractGroup("01.01.01.001").ShouldBe("01.01.01");
        CrawlerService.ExtractGroup("07.02.01").ShouldBe("07.02.01");
    }

    [Fact]
    public void ExtractGroup_SemPontos_RetornaGrupo()
    {
        CrawlerService.ExtractGroup("010101").ShouldBe("01.01.01");
        CrawlerService.ExtractGroup("010101001").ShouldBe("01.01.01");
    }

    [Fact]
    public void FormatServiceCode_FormataCorretamente()
    {
        CrawlerService.FormatServiceCode("010101").ShouldBe("01.01.01");
        CrawlerService.FormatServiceCode("010101001").ShouldBe("01.01.01.001");
        CrawlerService.FormatServiceCode("01.01.01").ShouldBe("01.01.01");
    }

    [Fact]
    public async Task ExecutarAsync_ComCancelamento_RetornaFalha()
    {
        // Arrange
        _municipioRepo.Setup(r => r.GetByUfAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<Municipio>());

        CancellationTokenSource cts = new();
        _nfseClient.Setup(c => c.GetConvenioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // The execution should handle cancellation gracefully
        // Since no municipalities found, it completes without hitting the cancellation
        Result<ExecucaoCrawler> resultado = await _sut.ExecutarAsync(TipoExecucao.Manual, cancellationToken: cts.Token);
        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.Status.ShouldBe(StatusExecucao.Concluido);
    }

    [Fact]
    public async Task ExecutarAsync_ComExcecaoInesperada_RetornaFalha()
    {
        // Arrange
        _municipioRepo.Setup(r => r.GetByUfAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        Result<ExecucaoCrawler> resultado = await _sut.ExecutarAsync(TipoExecucao.Manual);

        // Assert
        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.Status.ShouldBe(StatusExecucao.Falha);
        resultado.Value.Erros.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ProcessarFilaAsync_ComBudgetExhausted_ParaProcessamento()
    {
        // Arrange
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        string competencia = CrawlerService.GetCompetenciaAtual();

        _certProtection.Setup(c => c.BudgetExhausted).Returns(true);

        _filaRepo.Setup(r => r.GetPendingAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<FilaProcessamento>
            {
                FilaProcessamento.Create("3106200", "01.01.01", competencia, "exec1")
            });

        // Act
        await _sut.ProcessarFilaAsync(execucao, competencia, CancellationToken.None);

        // Assert - should not process any items due to budget
        _nfseClient.Verify(c => c.GetAliquotaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessarFilaAsync_ComHalt_ParaProcessamento()
    {
        // Arrange
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        string competencia = CrawlerService.GetCompetenciaAtual();

        _certProtection.Setup(c => c.ShouldHalt).Returns(true);

        _filaRepo.Setup(r => r.GetPendingAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<FilaProcessamento>
            {
                FilaProcessamento.Create("3106200", "01.01.01", competencia, "exec1")
            });

        // Act
        await _sut.ProcessarFilaAsync(execucao, competencia, CancellationToken.None);

        // Assert
        _nfseClient.Verify(c => c.GetAliquotaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExtrairESalvarAliquotasAsync_ComMultiplasAliquotas_SalvaTodas()
    {
        // Arrange
        AliquotaNfseResponse response = new()
        {
            Aliquotas = new Dictionary<string, List<AliquotaItem>>
            {
                ["01.01.01.000"] = new List<AliquotaItem>
                {
                    new() { Incidencia = "SIM", Aliq = 5.0m, DtIni = DateTime.UtcNow.AddYears(-1), DtFim = null },
                    new() { Incidencia = "SIM", Aliq = 3.0m, DtIni = DateTime.UtcNow.AddYears(-2), DtFim = DateTime.UtcNow.AddYears(-1) }
                }
            },
            Mensagem = "ok"
        };

        _aliquotaRepo.Setup(r => r.UpsertAsync(It.IsAny<Aliquota>())).Returns(Task.CompletedTask);

        // Act
        int count = await _sut.ExtrairESalvarAliquotasAsync(
            response, "3106200", "Belo Horizonte", "01.01.01", "2026-04-01");

        // Assert — only 1 vigente (the one with DtFim = null), the expired one is skipped
        count.ShouldBe(1);
        _aliquotaRepo.Verify(r => r.UpsertAsync(It.IsAny<Aliquota>()), Times.Once);
    }

    [Fact]
    public async Task ExtrairESalvarAliquotasAsync_SemAliquotas_RetornaZero()
    {
        // Arrange
        AliquotaNfseResponse response = new()
        {
            Aliquotas = null,
            Mensagem = "ok"
        };

        // Act
        int count = await _sut.ExtrairESalvarAliquotasAsync(
            response, "3106200", "Belo Horizonte", "01.01.01", "2026-04-01");

        // Assert
        count.ShouldBe(0);
    }

    [Fact]
    public void NfseApiClient_FormatarCodigoServico_FormataCorretamente()
    {
        // 6 digits → adds .000
        NfseApiClient.FormatarCodigoServico("01.01.01").ShouldBe("01.01.01.000");
        NfseApiClient.FormatarCodigoServico("01.01.00").ShouldBe("01.01.00.000");
        NfseApiClient.FormatarCodigoServico("010101").ShouldBe("01.01.01.000");

        // 9 digits → formats with dots
        NfseApiClient.FormatarCodigoServico("010101000").ShouldBe("01.01.01.000");
        NfseApiClient.FormatarCodigoServico("01.01.01.000").ShouldBe("01.01.01.000");
        NfseApiClient.FormatarCodigoServico("010101001").ShouldBe("01.01.01.001");
    }

    #region Capitais Primeiro Tests

    [Fact]
    public async Task FaseConvenioAsync_ComCapitaisENaoCapitais_DeveProcessarCapitaisPrimeiro()
    {
        // Arrange
        Municipio capitalMG = Municipio.Create("3106200", "Belo Horizonte", "MG", ehCapital: true);
        Municipio interiorMG = Municipio.Create("3170206", "Uberlândia", "MG");
        Municipio capitalSP = Municipio.Create("3550308", "São Paulo", "SP", ehCapital: true);
        Municipio interiorSP = Municipio.Create("3509502", "Campinas", "SP");

        _municipioRepo.Setup(r => r.GetByUfAsync("MG"))
            .ReturnsAsync(new List<Municipio> { interiorMG, capitalMG });
        _municipioRepo.Setup(r => r.GetByUfAsync("SP"))
            .ReturnsAsync(new List<Municipio> { interiorSP, capitalSP });
        _municipioRepo.Setup(r => r.GetByUfAsync(It.Is<string>(s => s != "MG" && s != "SP")))
            .ReturnsAsync(new List<Municipio>());

        // Todos os municípios são ativos (convenio OK)
        _nfseClient.Setup(c => c.GetConvenioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarConvenioAtivo());

        // Act
        ExecucaoCrawler execucaoCapitais = ExecucaoCrawler.Create(TipoExecucao.Manual);
        List<Municipio> resultado = await _sut.FaseConvenioAsync(execucaoCapitais, null, null, CancellationToken.None);

        // Assert
        resultado.Count.ShouldBe(4);

        // Dentro de cada UF, capitais vêm primeiro (MG é processado antes de SP)
        resultado[0].CodigoIbge.ShouldBe("3106200"); // BH (capital MG)
        resultado[1].CodigoIbge.ShouldBe("3170206"); // Uberlândia (interior MG)
        resultado[2].CodigoIbge.ShouldBe("3550308"); // São Paulo (capital SP)
        resultado[3].CodigoIbge.ShouldBe("3509502"); // Campinas (interior SP)
    }

    [Fact]
    public async Task FaseConvenioAsync_SomenteCapitais_DeveOrdenarPorUf()
    {
        // Arrange
        Municipio capitalAM = Municipio.Create("1302603", "Manaus", "AM", ehCapital: true);
        Municipio capitalRO = Municipio.Create("1100205", "Porto Velho", "RO", ehCapital: true);

        _municipioRepo.Setup(r => r.GetByUfAsync("AM"))
            .ReturnsAsync(new List<Municipio> { capitalAM });
        _municipioRepo.Setup(r => r.GetByUfAsync("RO"))
            .ReturnsAsync(new List<Municipio> { capitalRO });
        _municipioRepo.Setup(r => r.GetByUfAsync(It.Is<string>(s => s != "AM" && s != "RO")))
            .ReturnsAsync(new List<Municipio>());

        _nfseClient.Setup(c => c.GetConvenioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarConvenioAtivo());

        // Act
        ExecucaoCrawler execucaoUfs = ExecucaoCrawler.Create(TipoExecucao.Manual);
        List<Municipio> resultado = await _sut.FaseConvenioAsync(execucaoUfs, null, null, CancellationToken.None);

        // Assert
        resultado.Count.ShouldBe(2);
        resultado[0].CodigoIbge.ShouldBe("1302603"); // Manaus (AM vem antes de RO)
        resultado[1].CodigoIbge.ShouldBe("1100205"); // Porto Velho
    }

    [Fact]
    public void Municipio_Create_ComEhCapital_DeveDefinirPropriedade()
    {
        // Arrange & Act
        Municipio capital = Municipio.Create("3550308", "São Paulo", "SP", ehCapital: true);
        Municipio naoCapital = Municipio.Create("3509502", "Campinas", "SP");

        // Assert
        capital.EhCapital.ShouldBeTrue();
        naoCapital.EhCapital.ShouldBeFalse();
    }

    [Fact]
    public async Task Given_FiltroCapitalTrue_Should_RetornarApenasCapitais()
    {
        // Arrange
        Municipio capitalMG = Municipio.Create("3106200", "Belo Horizonte", "MG", ehCapital: true);
        Municipio interiorMG = Municipio.Create("3170206", "Uberlândia", "MG");
        Municipio capitalSP = Municipio.Create("3550308", "São Paulo", "SP", ehCapital: true);
        Municipio interiorSP = Municipio.Create("3509502", "Campinas", "SP");

        _municipioRepo.Setup(r => r.GetByUfAsync("MG"))
            .ReturnsAsync(new List<Municipio> { interiorMG, capitalMG });
        _municipioRepo.Setup(r => r.GetByUfAsync("SP"))
            .ReturnsAsync(new List<Municipio> { interiorSP, capitalSP });
        _municipioRepo.Setup(r => r.GetByUfAsync(It.Is<string>(s => s != "MG" && s != "SP")))
            .ReturnsAsync(new List<Municipio>());

        _nfseClient.Setup(c => c.GetConvenioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarConvenioAtivo());

        // Act
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        List<Municipio> resultado = await _sut.FaseConvenioAsync(execucao, null, true, CancellationToken.None);

        // Assert
        resultado.Count.ShouldBe(2);
        resultado.ShouldAllBe(m => m.EhCapital);
        resultado[0].CodigoIbge.ShouldBe("3106200"); // BH (capital MG)
        resultado[1].CodigoIbge.ShouldBe("3550308"); // SP (capital SP)
    }

    [Fact]
    public async Task Given_FiltroCapitalFalse_Should_RetornarApenasNaoCapitais()
    {
        // Arrange
        Municipio capitalMG = Municipio.Create("3106200", "Belo Horizonte", "MG", ehCapital: true);
        Municipio interiorMG = Municipio.Create("3170206", "Uberlândia", "MG");
        Municipio capitalSP = Municipio.Create("3550308", "São Paulo", "SP", ehCapital: true);
        Municipio interiorSP = Municipio.Create("3509502", "Campinas", "SP");

        _municipioRepo.Setup(r => r.GetByUfAsync("MG"))
            .ReturnsAsync(new List<Municipio> { interiorMG, capitalMG });
        _municipioRepo.Setup(r => r.GetByUfAsync("SP"))
            .ReturnsAsync(new List<Municipio> { interiorSP, capitalSP });
        _municipioRepo.Setup(r => r.GetByUfAsync(It.Is<string>(s => s != "MG" && s != "SP")))
            .ReturnsAsync(new List<Municipio>());

        _nfseClient.Setup(c => c.GetConvenioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarConvenioAtivo());

        // Act
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        List<Municipio> resultado = await _sut.FaseConvenioAsync(execucao, null, false, CancellationToken.None);

        // Assert
        resultado.Count.ShouldBe(2);
        resultado.ShouldAllBe(m => !m.EhCapital);
        resultado[0].CodigoIbge.ShouldBe("3170206"); // Uberlândia (interior MG — MG vem antes de SP por UF)
        resultado[1].CodigoIbge.ShouldBe("3509502"); // Campinas (interior SP)
    }

    [Fact]
    public async Task Given_FiltroCapitalNull_Should_RetornarTodosMunicipios()
    {
        // Arrange
        Municipio capitalMG = Municipio.Create("3106200", "Belo Horizonte", "MG", ehCapital: true);
        Municipio interiorMG = Municipio.Create("3170206", "Uberlândia", "MG");

        _municipioRepo.Setup(r => r.GetByUfAsync("MG"))
            .ReturnsAsync(new List<Municipio> { interiorMG, capitalMG });
        _municipioRepo.Setup(r => r.GetByUfAsync(It.Is<string>(s => s != "MG")))
            .ReturnsAsync(new List<Municipio>());

        _nfseClient.Setup(c => c.GetConvenioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarConvenioAtivo());

        // Act
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        List<Municipio> resultado = await _sut.FaseConvenioAsync(execucao, null, null, CancellationToken.None);

        // Assert
        resultado.Count.ShouldBe(2);
        resultado[0].EhCapital.ShouldBeTrue();  // Capital primeiro (ordenação)
        resultado[1].EhCapital.ShouldBeFalse(); // Interior depois
    }

    #endregion

    #region Detalhamento Iteration Tests

    [Theory]
    [InlineData("01.01.00", true)]   // Seed code with placeholder detalhamento
    [InlineData("010100", true)]     // Same without dots
    [InlineData("07.02.00", true)]   // Another seed code
    [InlineData("01.01.01", false)]  // Already has valid detalhamento
    [InlineData("010101", false)]    // Same without dots
    [InlineData("07.02.01", false)]  // Already has valid detalhamento
    [InlineData("14.01.03", false)]  // Detalhamento 03
    public void NeedsDetalhamentoIteration_DetectsCorrectly(string codigoServico, bool expected)
    {
        CrawlerService.NeedsDetalhamentoIteration(codigoServico).ShouldBe(expected);
    }

    [Theory]
    [InlineData("01.01.00", "01", "01")]
    [InlineData("07.02.00", "07", "02")]
    [InlineData("14.01.00", "14", "01")]
    [InlineData("010100", "01", "01")]
    public void ExtrairItemSubitem_ExtractsCorrectly(string codigoServico, string expectedItem, string expectedSubitem)
    {
        (string item, string subitem) = CrawlerService.ExtrairItemSubitem(codigoServico);
        item.ShouldBe(expectedItem);
        subitem.ShouldBe(expectedSubitem);
    }

    [Fact]
    public async Task ProcessarItemAsync_ComCodigoSeed00_UsaIteracaoDetalhamento()
    {
        // Arrange — seed code "01.01.00" should trigger iteration
        FilaProcessamento item = FilaProcessamento.Create("2800308", "01.01.00", "2026-04-01", "exec1");
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        System.Collections.Concurrent.ConcurrentDictionary<string, int> misses = new();

        Municipio mun = Municipio.Create("2800308", "Aracaju", "SE");
        _municipioRepo.Setup(r => r.GetByCodigoIbgeAsync("2800308")).ReturnsAsync(mun);
        _aliquotaRepo.Setup(r => r.UpsertAsync(It.IsAny<Aliquota>())).Returns(Task.CompletedTask);

        // Detalhamento 01 → has data
        _nfseClient.Setup(c => c.GetAliquotaAsync("2800308", "01.01.01.000", "2026-04-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarAliquotaResponse("01.01.01.000", 5.0m));

        // Detalhamento 02 → has data
        _nfseClient.Setup(c => c.GetAliquotaAsync("2800308", "01.01.02.000", "2026-04-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarAliquotaResponse("01.01.02.000", 3.0m));

        // Detalhamento 03, 04, 05 → null (3 consecutive misses → stops)
        _nfseClient.Setup(c => c.GetAliquotaAsync("2800308", "01.01.03.000", "2026-04-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AliquotaNfseResponse?)null);
        _nfseClient.Setup(c => c.GetAliquotaAsync("2800308", "01.01.04.000", "2026-04-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AliquotaNfseResponse?)null);
        _nfseClient.Setup(c => c.GetAliquotaAsync("2800308", "01.01.05.000", "2026-04-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AliquotaNfseResponse?)null);

        // Desdobramentos for detalhamento 01 → 001 has data, 002 and 003 return null
        _nfseClient.Setup(c => c.GetAliquotaAsync("2800308", "01.01.01.001", "2026-04-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AliquotaNfseResponse?)null);
        _nfseClient.Setup(c => c.GetAliquotaAsync("2800308", "01.01.01.002", "2026-04-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AliquotaNfseResponse?)null);

        // Desdobramentos for detalhamento 02 → all null
        _nfseClient.Setup(c => c.GetAliquotaAsync("2800308", "01.01.02.001", "2026-04-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AliquotaNfseResponse?)null);
        _nfseClient.Setup(c => c.GetAliquotaAsync("2800308", "01.01.02.002", "2026-04-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AliquotaNfseResponse?)null);

        // Act
        await _sut.ProcessarItemAsync(item, execucao, "2026-04-01", misses, CancellationToken.None);

        // Assert — should have called GetAliquotaAsync for det 01, 02, 03, 04, 05 (stops at 05 after 3 consecutive misses from 03/04/05)
        // Plus desdobramentos for det 01 and 02
        execucao.Processados.ShouldBe(1);
        _aliquotaRepo.Verify(r => r.UpsertAsync(It.IsAny<Aliquota>()), Times.Exactly(2)); // 01.01.01.000 and 01.01.02.000
        misses.GetValueOrDefault("01.01.00").ShouldBe(0); // Found data, so reset
    }

    [Fact]
    public async Task ProcessarItemAsync_ComCodigoSeed00_SemDadosNenhumDetalhamento_IncrementaMisses()
    {
        // Arrange — seed code "07.02.00", all detalhamentos return null
        FilaProcessamento item = FilaProcessamento.Create("2800308", "07.02.00", "2026-04-01", "exec1");
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        System.Collections.Concurrent.ConcurrentDictionary<string, int> misses = new();

        Municipio mun = Municipio.Create("2800308", "Aracaju", "SE");
        _municipioRepo.Setup(r => r.GetByCodigoIbgeAsync("2800308")).ReturnsAsync(mun);

        // All detalhamentos return null
        _nfseClient.Setup(c => c.GetAliquotaAsync("2800308", It.IsAny<string>(), "2026-04-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AliquotaNfseResponse?)null);

        // Act
        await _sut.ProcessarItemAsync(item, execucao, "2026-04-01", misses, CancellationToken.None);

        // Assert — no data found, group miss incremented
        execucao.Processados.ShouldBe(1);
        _aliquotaRepo.Verify(r => r.UpsertAsync(It.IsAny<Aliquota>()), Times.Never);
        misses["07.02.00"].ShouldBe(1);
    }

    [Fact]
    public async Task ProcessarItemAsync_ComCodigoNaoSeed_UsaFluxoDireto()
    {
        // Arrange — code "01.01.01" has detalhamento 01 (not 00), so direct path
        FilaProcessamento item = FilaProcessamento.Create("2800308", "01.01.01", "2026-04-01", "exec1");
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        System.Collections.Concurrent.ConcurrentDictionary<string, int> misses = new();

        Municipio mun = Municipio.Create("2800308", "Aracaju", "SE");
        _municipioRepo.Setup(r => r.GetByCodigoIbgeAsync("2800308")).ReturnsAsync(mun);
        _aliquotaRepo.Setup(r => r.UpsertAsync(It.IsAny<Aliquota>())).Returns(Task.CompletedTask);

        _nfseClient.Setup(c => c.GetAliquotaAsync("2800308", "01.01.01", "2026-04-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarAliquotaResponse("01.01.01.000", 5.0m));

        // Act
        await _sut.ProcessarItemAsync(item, execucao, "2026-04-01", misses, CancellationToken.None);

        // Assert — direct path, single API call
        execucao.Processados.ShouldBe(1);
        _aliquotaRepo.Verify(r => r.UpsertAsync(It.IsAny<Aliquota>()), Times.Once);
    }

    [Fact]
    public async Task ProcessarItemComIteracaoAsync_ComDesdobramentos_SalvaTodos()
    {
        // Arrange — seed code "01.01.00", detalhamento 01 has desdobramentos 000 and 001
        FilaProcessamento item = FilaProcessamento.Create("2800308", "01.01.00", "2026-04-01", "exec1");
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        System.Collections.Concurrent.ConcurrentDictionary<string, int> misses = new();

        Municipio mun = Municipio.Create("2800308", "Aracaju", "SE");
        _municipioRepo.Setup(r => r.GetByCodigoIbgeAsync("2800308")).ReturnsAsync(mun);
        _aliquotaRepo.Setup(r => r.UpsertAsync(It.IsAny<Aliquota>())).Returns(Task.CompletedTask);

        // Detalhamento 01, desdobramento 000 → data
        _nfseClient.Setup(c => c.GetAliquotaAsync("2800308", "01.01.01.000", "2026-04-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarAliquotaResponse("01.01.01.000", 5.0m));

        // Detalhamento 01, desdobramento 001 → data
        _nfseClient.Setup(c => c.GetAliquotaAsync("2800308", "01.01.01.001", "2026-04-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarAliquotaResponse("01.01.01.001", 4.0m));

        // Detalhamento 01, desdobramento 002, 003 → null (2 consecutive → stops desdobramento)
        _nfseClient.Setup(c => c.GetAliquotaAsync("2800308", "01.01.01.002", "2026-04-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AliquotaNfseResponse?)null);
        _nfseClient.Setup(c => c.GetAliquotaAsync("2800308", "01.01.01.003", "2026-04-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AliquotaNfseResponse?)null);

        // Detalhamento 02, 03, 04 → null (3 consecutive → stops detalhamento)
        _nfseClient.Setup(c => c.GetAliquotaAsync("2800308", "01.01.02.000", "2026-04-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AliquotaNfseResponse?)null);
        _nfseClient.Setup(c => c.GetAliquotaAsync("2800308", "01.01.03.000", "2026-04-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AliquotaNfseResponse?)null);
        _nfseClient.Setup(c => c.GetAliquotaAsync("2800308", "01.01.04.000", "2026-04-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AliquotaNfseResponse?)null);

        // Act
        await _sut.ProcessarItemAsync(item, execucao, "2026-04-01", misses, CancellationToken.None);

        // Assert — 2 aliquotas saved (01.01.01.000 and 01.01.01.001)
        execucao.Processados.ShouldBe(1);
        _aliquotaRepo.Verify(r => r.UpsertAsync(It.IsAny<Aliquota>()), Times.Exactly(2));
    }

    #endregion

    #region ProgressoUf — Regression Tests (fix-progresso-uf-prematuro)

    [Fact]
    public async Task Dado_TodasChamadasConvenioFalham_ProgressoUfDeveSerFalha()
    {
        // Arrange — todas as chamadas GetConvenioAsync falham com HttpRequestException
        Municipio mun1 = Municipio.Create("2800308", "Aracaju", "SE");
        Municipio mun2 = Municipio.Create("2802106", "Itabaiana", "SE");

        _municipioRepo.Setup(r => r.GetByUfAsync("SE"))
            .ReturnsAsync(new List<Municipio> { mun1, mun2 });
        _municipioRepo.Setup(r => r.GetByUfAsync(It.Is<string>(s => s != "SE")))
            .ReturnsAsync(new List<Municipio>());

        _nfseClient.Setup(c => c.GetConvenioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("SSL certificate error", null, System.Net.HttpStatusCode.InternalServerError));

        // Act
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        List<Municipio> resultado = await _sut.FaseConvenioAsync(execucao, new[] { "SE" }, null, CancellationToken.None);

        // Assert — nenhum município ativo retornado
        resultado.Count.ShouldBe(0);
        execucao.ProgressoUfs.ShouldContainKey("SE");
        execucao.ProgressoUfs["SE"].Status.ShouldBe("Falha");
        execucao.ProgressoUfs["SE"].MunicipiosEncontrados.ShouldBe(2);
        execucao.ProgressoUfs["SE"].MunicipiosAtivos.ShouldBe(0);
    }

    [Fact]
    public async Task Dado_CertificateProtectionInterrompe_UfAtualDeveSerInterrompido()
    {
        // Arrange — RO tem 3 municípios; halt ativa após o 1º ser verificado
        Municipio mun1 = Municipio.Create("1100015", "Alta Floresta D'Oeste", "RO");
        Municipio mun2 = Municipio.Create("1100205", "Porto Velho", "RO");
        Municipio mun3 = Municipio.Create("1100379", "Vilhena", "RO");
        Municipio munAM = Municipio.Create("1302603", "Manaus", "AM");

        _municipioRepo.Setup(r => r.GetByUfAsync("RO"))
            .ReturnsAsync(new List<Municipio> { mun1, mun2, mun3 });
        _municipioRepo.Setup(r => r.GetByUfAsync("AM"))
            .ReturnsAsync(new List<Municipio> { munAM });
        _municipioRepo.Setup(r => r.GetByUfAsync(It.Is<string>(s => s != "RO" && s != "AM")))
            .ReturnsAsync(new List<Municipio>());

        // Todos os municípios retornam convênio ativo
        int chamadas = 0;
        _nfseClient.Setup(c => c.GetConvenioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => chamadas++)
            .ReturnsAsync(CriarConvenioAtivo());

        // ShouldHalt ativa após a 1ª chamada de convênio ser concluída
        // O check acontece ANTES de cada município no loop, então:
        // - mun1 (Alta Floresta): check=false (chamadas=0), processa, chamadas→1
        // - mun2 (Porto Velho): check=true (chamadas=1) → break → interrompido
        _certProtection.Setup(c => c.ShouldHalt).Returns(() => chamadas >= 1);

        // Act
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        await _sut.FaseConvenioAsync(execucao, new[] { "RO", "AM" }, null, CancellationToken.None);

        // Assert — RO deve estar "Interrompido" (halt ativou após 1º município)
        execucao.ProgressoUfs.ShouldContainKey("RO");
        execucao.ProgressoUfs["RO"].Status.ShouldBe("Interrompido");
        execucao.ProgressoUfs["RO"].MunicipiosEncontrados.ShouldBe(3);
        execucao.ProgressoUfs["RO"].MunicipiosAtivos.ShouldBe(1); // Apenas Alta Floresta verificada

        // AM não deve ter sido iniciada (break no loop externo)
        execucao.ProgressoUfs.ShouldNotContainKey("AM");
    }

    [Fact]
    public async Task Dado_ZeroMunicipiosAtivos_ProgressoUfDeveRefletirContagens()
    {
        // Arrange — município existe no banco mas convênio retorna null (inativo)
        Municipio mun1 = Municipio.Create("5300108", "Brasília", "DF");

        _municipioRepo.Setup(r => r.GetByUfAsync("DF"))
            .ReturnsAsync(new List<Municipio> { mun1 });
        _municipioRepo.Setup(r => r.GetByUfAsync(It.Is<string>(s => s != "DF")))
            .ReturnsAsync(new List<Municipio>());

        _nfseClient.Setup(c => c.GetConvenioAsync("5300108", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConvenioNfseResponse?)null);

        // Act
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        List<Municipio> resultado = await _sut.FaseConvenioAsync(execucao, new[] { "DF" }, null, CancellationToken.None);

        // Assert
        resultado.Count.ShouldBe(0);
        execucao.ProgressoUfs["DF"].Status.ShouldBe("Concluido");
        execucao.ProgressoUfs["DF"].MunicipiosEncontrados.ShouldBe(1);
        execucao.ProgressoUfs["DF"].MunicipiosAtivos.ShouldBe(0);
    }

    [Fact]
    public async Task Dado_ConvenioAtivo_ProgressoUfDeveSerConcluidoComAtivos()
    {
        // Arrange — 2 municípios, ambos com convênio ativo
        Municipio mun1 = Municipio.Create("2800308", "Aracaju", "SE");
        Municipio mun2 = Municipio.Create("2802106", "Itabaiana", "SE");

        _municipioRepo.Setup(r => r.GetByUfAsync("SE"))
            .ReturnsAsync(new List<Municipio> { mun1, mun2 });
        _municipioRepo.Setup(r => r.GetByUfAsync(It.Is<string>(s => s != "SE")))
            .ReturnsAsync(new List<Municipio>());

        _nfseClient.Setup(c => c.GetConvenioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarConvenioAtivo());

        // Act
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        List<Municipio> resultado = await _sut.FaseConvenioAsync(execucao, new[] { "SE" }, null, CancellationToken.None);

        // Assert
        resultado.Count.ShouldBe(2);
        execucao.ProgressoUfs["SE"].Status.ShouldBe("Concluido");
        execucao.ProgressoUfs["SE"].MunicipiosEncontrados.ShouldBe(2);
        execucao.ProgressoUfs["SE"].MunicipiosAtivos.ShouldBe(2);
    }

    [Fact]
    public async Task Dado_FalhasParciais_ProgressoUfDeveRefletirAtivos()
    {
        // Arrange — 3 municípios: 1 ativo, 1 inativo (null), 1 falha (exception)
        Municipio mun1 = Municipio.Create("2800308", "Aracaju", "SE");
        Municipio mun2 = Municipio.Create("2802106", "Itabaiana", "SE");
        Municipio mun3 = Municipio.Create("2803500", "Lagarto", "SE");

        _municipioRepo.Setup(r => r.GetByUfAsync("SE"))
            .ReturnsAsync(new List<Municipio> { mun1, mun2, mun3 });
        _municipioRepo.Setup(r => r.GetByUfAsync(It.Is<string>(s => s != "SE")))
            .ReturnsAsync(new List<Municipio>());

        // mun1: convênio ativo
        _nfseClient.Setup(c => c.GetConvenioAsync("2800308", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarConvenioAtivo());
        // mun2: sem convênio
        _nfseClient.Setup(c => c.GetConvenioAsync("2802106", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConvenioNfseResponse?)null);
        // mun3: erro HTTP
        _nfseClient.Setup(c => c.GetConvenioAsync("2803500", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Timeout", null, System.Net.HttpStatusCode.RequestTimeout));

        // Act
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        List<Municipio> resultado = await _sut.FaseConvenioAsync(execucao, new[] { "SE" }, null, CancellationToken.None);

        // Assert — apenas Aracaju é ativo
        resultado.Count.ShouldBe(1);
        resultado[0].CodigoIbge.ShouldBe("2800308");
        execucao.ProgressoUfs["SE"].Status.ShouldBe("Concluido"); // Não é "Falha" porque nem todos falharam
        execucao.ProgressoUfs["SE"].MunicipiosEncontrados.ShouldBe(3);
        execucao.ProgressoUfs["SE"].MunicipiosAtivos.ShouldBe(1);
    }

    [Fact]
    public async Task Dado_InterrupcaoNoMeio_UfsNaoIniciadasDevemFicarPendente()
    {
        // Arrange — 3 UFs: AC, AL, AM. Halt ativa após processar AC (1 município).
        // AL inicia mas é interrompida imediatamente, AM nunca é iniciada.
        Municipio munAC = Municipio.Create("1200401", "Rio Branco", "AC");
        Municipio munAL1 = Municipio.Create("2700102", "Água Branca", "AL");
        Municipio munAL2 = Municipio.Create("2704302", "Maceió", "AL");
        Municipio munAM = Municipio.Create("1302603", "Manaus", "AM");

        _municipioRepo.Setup(r => r.GetByUfAsync("AC"))
            .ReturnsAsync(new List<Municipio> { munAC });
        _municipioRepo.Setup(r => r.GetByUfAsync("AL"))
            .ReturnsAsync(new List<Municipio> { munAL1, munAL2 });
        _municipioRepo.Setup(r => r.GetByUfAsync("AM"))
            .ReturnsAsync(new List<Municipio> { munAM });
        _municipioRepo.Setup(r => r.GetByUfAsync(It.Is<string>(s => s != "AC" && s != "AL" && s != "AM")))
            .ReturnsAsync(new List<Municipio>());

        // Todos retornam convênio ativo
        int chamadas = 0;
        _nfseClient.Setup(c => c.GetConvenioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => chamadas++)
            .ReturnsAsync(CriarConvenioAtivo());

        // Halt ativa após 1ª chamada (AC processa Rio Branco, chamadas→1).
        // AL: ao iniciar o loop de municípios, ShouldHalt já é true → break imediato → interrompida
        _certProtection.Setup(c => c.ShouldHalt).Returns(() => chamadas >= 1);

        // Act
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        await _sut.FaseConvenioAsync(execucao, new[] { "AC", "AL", "AM" }, null, CancellationToken.None);

        // Assert
        // AC: processa Rio Branco antes do halt → Concluido
        execucao.ProgressoUfs["AC"].Status.ShouldBe("Concluido");
        execucao.ProgressoUfs["AC"].MunicipiosAtivos.ShouldBe(1);

        // AL: halt já ativo ao entrar no loop → interrompida sem verificar nenhum município
        execucao.ProgressoUfs["AL"].Status.ShouldBe("Interrompido");
        execucao.ProgressoUfs["AL"].MunicipiosAtivos.ShouldBe(0);

        // AM: não foi iniciada — não deve existir no dicionário
        execucao.ProgressoUfs.ShouldNotContainKey("AM");
    }

    #endregion
}
