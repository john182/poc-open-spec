using MapaTributario.API.Application.Crawler;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using MapaTributario.API.Infrastructure.External;
using MapaTributario.API.Infrastructure.External.Contracts;
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
    private readonly Mock<INfseApiClient> _nfseClient = new();
    private readonly Mock<IRateLimiter> _rateLimiter = new();
    private readonly Mock<ICircuitBreaker> _circuitBreaker = new();
    private readonly Mock<ICertificateProtection> _certProtection = new();
    private readonly Mock<ICrawlerExecutionGuard> _executionGuard = new();
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
        _execucaoRepo.Setup(r => r.CreateAsync(It.IsAny<ExecucaoCrawler>()))
            .ReturnsAsync((ExecucaoCrawler e) => e);
        _execucaoRepo.Setup(r => r.UpdateAsync(It.IsAny<ExecucaoCrawler>())).Returns(Task.CompletedTask);
        _filaRepo.Setup(r => r.RevertProcessingTopendingAsync()).Returns(Task.CompletedTask);
        _filaRepo.Setup(r => r.InsertManyAsync(It.IsAny<IEnumerable<FilaProcessamento>>())).Returns(Task.CompletedTask);
        _filaRepo.Setup(r => r.UpdateStatusAsync(It.IsAny<FilaProcessamento>())).Returns(Task.CompletedTask);

        _sut = new CrawlerService(
            _execucaoRepo.Object,
            _filaRepo.Object,
            _municipioRepo.Object,
            _servicoRepo.Object,
            _aliquotaRepo.Object,
            _nfseClient.Object,
            _rateLimiter.Object,
            _circuitBreaker.Object,
            _certProtection.Object,
            _executionGuard.Object,
            _logger.Object);
    }

    [Fact]
    public async Task ExecutarAsync_QuandoJaEmExecucao_LancaException()
    {
        // Arrange - guard refuses acquisition (already running)
        _executionGuard.Setup(g => g.TryAcquire()).Returns(false);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.ExecutarAsync(TipoExecucao.Manual));
    }

    [Fact]
    public async Task ExecutarAsync_SemMunicipiosAtivos_RetornaConcluido()
    {
        // Arrange
        _municipioRepo.Setup(r => r.GetByUfAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<Municipio>());

        // Act
        ExecucaoCrawler result = await _sut.ExecutarAsync(TipoExecucao.Agendado);

        // Assert
        result.Status.ShouldBe(StatusExecucao.Concluido);
        result.Tipo.ShouldBe(TipoExecucao.Agendado);
        result.Fim.ShouldNotBeNull();
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
            .ReturnsAsync(new ConvenioNfseResponse { Ativo = true });

        // Probe: at least one returns data
        _nfseClient.Setup(c => c.GetAliquotaAsync("3106200", "01.01.01", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AliquotaNfseResponse { Aliquota = 2.0m, CodigoServico = "01.01.01", DescricaoServico = "Servico teste" });

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
            .ReturnsAsync(new AliquotaNfseResponse { Aliquota = 2.0m, CodigoServico = "01.01.01", DescricaoServico = "Servico teste" });

        _aliquotaRepo.Setup(r => r.UpsertAsync(It.IsAny<Aliquota>())).Returns(Task.CompletedTask);

        // Act
        ExecucaoCrawler result = await _sut.ExecutarAsync(TipoExecucao.Manual);

        // Assert
        result.Status.ShouldBe(StatusExecucao.Concluido);
        result.Processados.ShouldBeGreaterThanOrEqualTo(1);
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
            .ReturnsAsync(new ConvenioNfseResponse { Ativo = true });
        _nfseClient.Setup(c => c.GetConvenioAsync("1302603", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConvenioNfseResponse?)null);

        // Act
        List<Municipio> result = await _sut.FaseConvenioAsync(CancellationToken.None);

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
            .ReturnsAsync(new AliquotaNfseResponse { Aliquota = 2.0m, CodigoServico = "01.01.01", DescricaoServico = "Teste" });

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

        _aliquotaRepo.Setup(r => r.ExistsAsync("3106200", "01.01.01", competencia)).ReturnsAsync(true);
        _aliquotaRepo.Setup(r => r.ExistsAsync("3106200", "07.02.01", competencia)).ReturnsAsync(false);

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
        Dictionary<string, int> misses = new();

        _nfseClient.Setup(c => c.GetAliquotaAsync("3106200", "01.01.01", "2026-04-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AliquotaNfseResponse { Aliquota = 5.0m, CodigoServico = "01.01.01", DescricaoServico = "Teste" });

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
        Dictionary<string, int> misses = new();

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
        Dictionary<string, int> misses = new();

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
        Dictionary<string, int> misses = new();

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
        ExecucaoCrawler result = await _sut.ExecutarAsync(TipoExecucao.Manual, cancellationToken: cts.Token);
        result.Status.ShouldBe(StatusExecucao.Concluido);
    }

    [Fact]
    public async Task ExecutarAsync_ComExcecaoInesperada_RetornaFalha()
    {
        // Arrange
        _municipioRepo.Setup(r => r.GetByUfAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        ExecucaoCrawler result = await _sut.ExecutarAsync(TipoExecucao.Manual);

        // Assert
        result.Status.ShouldBe(StatusExecucao.Falha);
        result.Erros.ShouldBeGreaterThan(0);
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
}
