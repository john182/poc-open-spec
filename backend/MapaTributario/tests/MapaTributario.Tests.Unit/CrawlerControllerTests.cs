#pragma warning disable CS8604 // ToString() em objetos anônimos retorna string? mas nunca é null nos testes
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MapaTributario.API.Application.Crawler;
using MapaTributario.API.Application.Crawler.Contracts;
using MapaTributario.API.Application.Errors;
using MapaTributario.API.Controllers;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class CrawlerControllerTests
{
    private readonly Mock<ICrawlerExecutionGuard> _executionGuard = new();
    private readonly Mock<IExecucaoCrawlerRepository> _execucaoRepository = new();
    private readonly Mock<IServiceProvider> _serviceProvider = new();
    private readonly Mock<ICertificadoStore> _certificadoStore = new();
    private readonly Mock<IConfiguracaoCrawlerAppService> _configuracaoAppService = new();
    private readonly Mock<IValidator<AtualizarConfiguracaoCrawlerRequest>> _atualizarValidator = new();
    private readonly Mock<IValidator<AtualizarParcialConfiguracaoCrawlerRequest>> _atualizarParcialValidator = new();

    private CrawlerController CriarSut()
    {
        return new CrawlerController(
            _executionGuard.Object,
            _execucaoRepository.Object,
            _serviceProvider.Object,
            _certificadoStore.Object,
            _configuracaoAppService.Object);
    }

    private void ConfigurarServiceProviderComCrawlerService()
    {
        Mock<ICrawlerService> crawlerService = new();
        Mock<IServiceScope> serviceScope = new();
        Mock<IServiceProvider> scopeServiceProvider = new();
        Mock<IServiceScopeFactory> serviceScopeFactory = new();

        scopeServiceProvider.Setup(sp => sp.GetService(typeof(ICrawlerService)))
            .Returns(crawlerService.Object);
        serviceScope.Setup(s => s.ServiceProvider).Returns(scopeServiceProvider.Object);
        serviceScopeFactory.Setup(f => f.CreateScope()).Returns(serviceScope.Object);
        _serviceProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(serviceScopeFactory.Object);
    }

    private void ConfigurarValidacaoComSucesso<T>(Mock<IValidator<T>> validatorMock)
    {
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private void ConfigurarValidacaoComFalha<T>(Mock<IValidator<T>> validatorMock, string mensagemErro = "Campo inválido")
    {
        ValidationResult resultado = new(new[]
        {
            new ValidationFailure("Campo", mensagemErro)
        });
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultado);
    }

    private static ExecucaoCrawler CriarExecucaoFinalizada()
    {
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        execucao.SetId("exec-001");
        execucao.SetTotais(10, 100);
        execucao.Finalizar(StatusExecucao.Concluido);
        return execucao;
    }

    // ── Executar ────────────────────────────────────────────────────

    [Fact]
    public void Given_SemCertificado_Should_RetornarBadRequest()
    {
        // Arrange
        CrawlerController sut = CriarSut();
        _certificadoStore.Setup(c => c.HasCertificate()).Returns(false);

        // Act
        IActionResult resultado = sut.Executar(null);

        // Assert
        BadRequestObjectResult badRequest = resultado.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value!.ToString().ShouldContain("Nenhum certificado digital disponível");
    }

    [Fact]
    public void Given_ExecucaoJaEmAndamento_Should_RetornarConflict()
    {
        // Arrange
        CrawlerController sut = CriarSut();
        _certificadoStore.Setup(c => c.HasCertificate()).Returns(true);
        _executionGuard.Setup(g => g.IsRunning).Returns(true);

        // Act
        IActionResult resultado = sut.Executar(null);

        // Assert
        ConflictObjectResult conflict = resultado.ShouldBeOfType<ConflictObjectResult>();
        conflict.Value!.ToString().ShouldContain("Uma execucao ja esta em andamento");
    }

    [Fact]
    public void Given_CertificadoEDisponivel_Should_RetornarAccepted()
    {
        // Arrange
        CrawlerController sut = CriarSut();
        _certificadoStore.Setup(c => c.HasCertificate()).Returns(true);
        _executionGuard.Setup(g => g.IsRunning).Returns(false);
        ConfigurarServiceProviderComCrawlerService();

        // Act
        IActionResult resultado = sut.Executar(new ExecutarCrawlerRequest());

        // Assert
        AcceptedResult accepted = resultado.ShouldBeOfType<AcceptedResult>();
        ExecutarCrawlerResponse resposta = accepted.Value.ShouldBeOfType<ExecutarCrawlerResponse>();
        resposta.Mensagem.ShouldBe("Execucao iniciada com sucesso");
    }

    [Fact]
    public void Given_CapitaisPrimeiro_Should_RetornarAcceptedComMensagemCapitais()
    {
        // Arrange
        CrawlerController sut = CriarSut();
        _certificadoStore.Setup(c => c.HasCertificate()).Returns(true);
        _executionGuard.Setup(g => g.IsRunning).Returns(false);
        ConfigurarServiceProviderComCrawlerService();
        ExecutarCrawlerRequest request = new() { CapitaisPrimeiro = true };

        // Act
        IActionResult resultado = sut.Executar(request);

        // Assert
        AcceptedResult accepted = resultado.ShouldBeOfType<AcceptedResult>();
        ExecutarCrawlerResponse resposta = accepted.Value.ShouldBeOfType<ExecutarCrawlerResponse>();
        resposta.Mensagem.ShouldContain("capitais primeiro");
    }

    [Fact]
    public void Given_RequestNulo_Should_RetornarAcceptedQuandoCertificadoDisponivel()
    {
        // Arrange
        CrawlerController sut = CriarSut();
        _certificadoStore.Setup(c => c.HasCertificate()).Returns(true);
        _executionGuard.Setup(g => g.IsRunning).Returns(false);
        ConfigurarServiceProviderComCrawlerService();

        // Act
        IActionResult resultado = sut.Executar(null);

        // Assert
        AcceptedResult accepted = resultado.ShouldBeOfType<AcceptedResult>();
        ExecutarCrawlerResponse resposta = accepted.Value.ShouldBeOfType<ExecutarCrawlerResponse>();
        resposta.Mensagem.ShouldBe("Execucao iniciada com sucesso");
    }

    // ── Status ──────────────────────────────────────────────────────

    [Fact]
    public async Task Given_SemExecucao_Should_RetornarOkComStatusNenhumaExecucao()
    {
        // Arrange
        CrawlerController sut = CriarSut();
        _execucaoRepository.Setup(r => r.GetLatestAsync()).ReturnsAsync((ExecucaoCrawler?)null);
        _certificadoStore.Setup(c => c.HasCertificate()).Returns(false);

        // Act
        IActionResult resultado = await sut.Status();

        // Assert
        OkObjectResult ok = resultado.ShouldBeOfType<OkObjectResult>();
        StatusCrawlerResponse status = ok.Value.ShouldBeOfType<StatusCrawlerResponse>();
        status.Status.ShouldBe("NenhumaExecucao");
        status.TotalMunicipios.ShouldBe(0);
    }

    [Fact]
    public async Task Given_ComExecucao_Should_RetornarOkComDadosDaExecucao()
    {
        // Arrange
        CrawlerController sut = CriarSut();
        ExecucaoCrawler execucao = CriarExecucaoFinalizada();
        _execucaoRepository.Setup(r => r.GetLatestAsync()).ReturnsAsync(execucao);
        _certificadoStore.Setup(c => c.HasCertificate()).Returns(true);

        // Act
        IActionResult resultado = await sut.Status();

        // Assert
        OkObjectResult ok = resultado.ShouldBeOfType<OkObjectResult>();
        StatusCrawlerResponse status = ok.Value.ShouldBeOfType<StatusCrawlerResponse>();
        status.Id.ShouldBe("exec-001");
        status.Status.ShouldBe("Concluido");
        status.TotalMunicipios.ShouldBe(10);
        status.TemCertificado.ShouldBeTrue();
    }

    // ── Execucoes ───────────────────────────────────────────────────

    [Fact]
    public async Task Given_ExecucoesRecentes_Should_RetornarOkComLista()
    {
        // Arrange
        CrawlerController sut = CriarSut();
        List<ExecucaoCrawler> execucoes = new() { CriarExecucaoFinalizada() };
        _execucaoRepository.Setup(r => r.GetRecentAsync(20))
            .ReturnsAsync(execucoes);
        _certificadoStore.Setup(c => c.HasCertificate()).Returns(true);

        // Act
        IActionResult resultado = await sut.Execucoes();

        // Assert
        OkObjectResult ok = resultado.ShouldBeOfType<OkObjectResult>();
        List<StatusCrawlerResponse> lista = ok.Value.ShouldBeOfType<List<StatusCrawlerResponse>>();
        lista.Count.ShouldBe(1);
        lista[0].Id.ShouldBe("exec-001");
    }

    // ── ObterConfiguracao ───────────────────────────────────────────

    [Fact]
    public async Task Given_ConfiguracaoExistente_Should_RetornarOk()
    {
        // Arrange
        CrawlerController sut = CriarSut();
        ConfiguracaoCrawlerResponse resposta = new()
        {
            Id = "config-001",
            CronSchedule = "0 3 * * *",
            LimiteRequisicoesPorSegundo = 5,
            Ativo = true
        };
        _configuracaoAppService.Setup(s => s.ObterConfiguracaoAtualAsync())
            .ReturnsAsync(Result.Ok(resposta));

        // Act
        IActionResult resultado = await sut.ObterConfiguracao();

        // Assert
        OkObjectResult ok = resultado.ShouldBeOfType<OkObjectResult>();
        ConfiguracaoCrawlerResponse valor = ok.Value.ShouldBeOfType<ConfiguracaoCrawlerResponse>();
        valor.Id.ShouldBe("config-001");
    }

    [Fact]
    public async Task Given_ConfiguracaoNaoEncontrada_Should_RetornarNotFound()
    {
        // Arrange
        CrawlerController sut = CriarSut();
        _configuracaoAppService.Setup(s => s.ObterConfiguracaoAtualAsync())
            .ReturnsAsync(Result.Fail<ConfiguracaoCrawlerResponse>(
                new NotFoundError("Nenhuma configuração ativa encontrada")));

        // Act
        IActionResult resultado = await sut.ObterConfiguracao();

        // Assert
        NotFoundObjectResult notFound = resultado.ShouldBeOfType<NotFoundObjectResult>();
        notFound.Value!.ToString().ShouldContain("Nenhuma configuração ativa encontrada");
    }

    // ── AtualizarConfiguracao ───────────────────────────────────────

    [Fact]
    public async Task Given_AtualizarConfiguracaoComValidacaoFalha_Should_RetornarBadRequest()
    {
        // Arrange
        CrawlerController sut = CriarSut();
        ConfigurarValidacaoComFalha(_atualizarValidator, "CronSchedule é obrigatório");
        AtualizarConfiguracaoCrawlerRequest request = new();

        // Act
        IActionResult resultado = await sut.AtualizarConfiguracao(request, _atualizarValidator.Object);

        // Assert
        BadRequestObjectResult badRequest = resultado.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value!.ToString().ShouldContain("Validação falhou");
    }

    [Fact]
    public async Task Given_AtualizarConfiguracaoComSucesso_Should_RetornarOk()
    {
        // Arrange
        CrawlerController sut = CriarSut();
        ConfigurarValidacaoComSucesso(_atualizarValidator);
        AtualizarConfiguracaoCrawlerRequest request = new() { CronSchedule = "0 3 * * *" };
        ConfiguracaoCrawlerResponse resposta = new() { Id = "config-001", CronSchedule = "0 3 * * *" };
        _configuracaoAppService.Setup(s => s.AtualizarConfiguracaoAsync(request))
            .ReturnsAsync(Result.Ok(resposta));

        // Act
        IActionResult resultado = await sut.AtualizarConfiguracao(request, _atualizarValidator.Object);

        // Assert
        OkObjectResult ok = resultado.ShouldBeOfType<OkObjectResult>();
        ConfiguracaoCrawlerResponse valor = ok.Value.ShouldBeOfType<ConfiguracaoCrawlerResponse>();
        valor.CronSchedule.ShouldBe("0 3 * * *");
    }

    [Fact]
    public async Task Given_AtualizarConfiguracaoNaoEncontrada_Should_RetornarNotFound()
    {
        // Arrange
        CrawlerController sut = CriarSut();
        ConfigurarValidacaoComSucesso(_atualizarValidator);
        AtualizarConfiguracaoCrawlerRequest request = new();
        _configuracaoAppService.Setup(s => s.AtualizarConfiguracaoAsync(request))
            .ReturnsAsync(Result.Fail<ConfiguracaoCrawlerResponse>(
                new NotFoundError("Nenhuma configuração ativa encontrada")));

        // Act
        IActionResult resultado = await sut.AtualizarConfiguracao(request, _atualizarValidator.Object);

        // Assert
        resultado.ShouldBeOfType<NotFoundObjectResult>();
    }

    // ── AtualizarParcialConfiguracao ────────────────────────────────

    [Fact]
    public async Task Given_AtualizarParcialComValidacaoFalha_Should_RetornarBadRequest()
    {
        // Arrange
        CrawlerController sut = CriarSut();
        ConfigurarValidacaoComFalha(_atualizarParcialValidator, "Valor inválido");
        AtualizarParcialConfiguracaoCrawlerRequest request = new();

        // Act
        IActionResult resultado = await sut.AtualizarParcialConfiguracao(request, _atualizarParcialValidator.Object);

        // Assert
        BadRequestObjectResult badRequest = resultado.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value!.ToString().ShouldContain("Validação falhou");
    }

    [Fact]
    public async Task Given_AtualizarParcialComSucesso_Should_RetornarOk()
    {
        // Arrange
        CrawlerController sut = CriarSut();
        ConfigurarValidacaoComSucesso(_atualizarParcialValidator);
        AtualizarParcialConfiguracaoCrawlerRequest request = new() { MaxTentativas = 5 };
        ConfiguracaoCrawlerResponse resposta = new() { Id = "config-001", MaxTentativas = 5 };
        _configuracaoAppService.Setup(s => s.AtualizarParcialmenteAsync(request))
            .ReturnsAsync(Result.Ok(resposta));

        // Act
        IActionResult resultado = await sut.AtualizarParcialConfiguracao(request, _atualizarParcialValidator.Object);

        // Assert
        OkObjectResult ok = resultado.ShouldBeOfType<OkObjectResult>();
        ConfiguracaoCrawlerResponse valor = ok.Value.ShouldBeOfType<ConfiguracaoCrawlerResponse>();
        valor.MaxTentativas.ShouldBe(5);
    }

    [Fact]
    public async Task Given_AtualizarParcialNaoEncontrada_Should_RetornarNotFound()
    {
        // Arrange
        CrawlerController sut = CriarSut();
        ConfigurarValidacaoComSucesso(_atualizarParcialValidator);
        AtualizarParcialConfiguracaoCrawlerRequest request = new();
        _configuracaoAppService.Setup(s => s.AtualizarParcialmenteAsync(request))
            .ReturnsAsync(Result.Fail<ConfiguracaoCrawlerResponse>(
                new NotFoundError("Nenhuma configuração ativa encontrada")));

        // Act
        IActionResult resultado = await sut.AtualizarParcialConfiguracao(request, _atualizarParcialValidator.Object);

        // Assert
        resultado.ShouldBeOfType<NotFoundObjectResult>();
    }
}
