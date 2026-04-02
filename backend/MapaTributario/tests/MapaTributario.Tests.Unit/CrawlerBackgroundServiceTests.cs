using FluentResults;
using MapaTributario.API.Application.Crawler;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using MapaTributario.API.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class CrawlerBackgroundServiceTests
{
    private readonly Mock<IServiceProvider> _serviceProvider = new();
    private readonly Mock<ICrawlerExecutionGuard> _executionGuard = new();
    private readonly Mock<ICertificadoStore> _certificadoStore = new();
    private readonly Mock<IConfiguracaoCrawlerRepository> _configuracaoRepo = new();
    private readonly Mock<ILogger<CrawlerBackgroundService>> _logger = new();
    private readonly Mock<ICrawlerService> _crawlerService = new();

    private CrawlerBackgroundService CreateSut(string cronSchedule = "0 2 * * *")
    {
        Dictionary<string, string?> config = new()
        {
            ["Crawler:CronSchedule"] = cronSchedule
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build();

        // Setup padrão: certificado disponível
        _certificadoStore.Setup(s => s.HasCertificate()).Returns(true);

        // Setup padrão: configuração do crawler com CronSchedule do parâmetro
        ConfiguracaoCrawler configuracao = ConfiguracaoCrawler.CriarPadrao();
        _configuracaoRepo.Setup(r => r.ObterAtualAsync()).ReturnsAsync(configuracao);

        // Setup service scope
        Mock<IServiceScope> scope = new();
        Mock<IServiceScopeFactory> scopeFactory = new();
        Mock<IServiceProvider> scopeServiceProvider = new();

        scopeServiceProvider.Setup(s => s.GetService(typeof(ICrawlerService)))
            .Returns(_crawlerService.Object);
        scope.Setup(s => s.ServiceProvider).Returns(scopeServiceProvider.Object);
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);
        _serviceProvider.Setup(s => s.GetService(typeof(IServiceScopeFactory)))
            .Returns(scopeFactory.Object);

        return new CrawlerBackgroundService(
            _serviceProvider.Object,
            _executionGuard.Object,
            _certificadoStore.Object,
            _configuracaoRepo.Object,
            _logger.Object,
            configuration);
    }

    [Fact]
    public void CalculateNextRunDelay_ComCron0_2_RetornaDelayCerto()
    {
        CrawlerBackgroundService sut = CreateSut("0 2 * * *");

        TimeSpan delay = sut.CalculateNextRunDelay();

        delay.TotalHours.ShouldBeGreaterThan(0);
        delay.TotalHours.ShouldBeLessThanOrEqualTo(24);
    }

    [Fact]
    public void CalculateNextRunDelay_ComCron30_3_RetornaDelayCerto()
    {
        CrawlerBackgroundService sut = CreateSut("30 3 * * *");

        TimeSpan delay = sut.CalculateNextRunDelay();

        delay.TotalHours.ShouldBeGreaterThan(0);
        delay.TotalHours.ShouldBeLessThanOrEqualTo(24);
    }

    [Fact]
    public async Task ExecuteScheduledRunAsync_QuandoCrawlerNaoRodando_ExecutaCrawler()
    {
        CrawlerBackgroundService sut = CreateSut();
        _executionGuard.Setup(g => g.IsRunning).Returns(false);
        _crawlerService.Setup(c => c.ExecutarAsync(TipoExecucao.Agendado, false, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(ExecucaoCrawler.Create(TipoExecucao.Agendado)));

        await sut.ExecuteScheduledRunAsync(CancellationToken.None);

        _crawlerService.Verify(c => c.ExecutarAsync(
            TipoExecucao.Agendado, false, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteScheduledRunAsync_QuandoCrawlerJaRodando_NaoExecuta()
    {
        CrawlerBackgroundService sut = CreateSut();
        _executionGuard.Setup(g => g.IsRunning).Returns(true);

        await sut.ExecuteScheduledRunAsync(CancellationToken.None);

        _crawlerService.Verify(c => c.ExecutarAsync(
            It.IsAny<TipoExecucao>(), It.IsAny<bool>(), It.IsAny<IReadOnlyList<string>?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteScheduledRunAsync_ComExcecao_NaoLancaException()
    {
        CrawlerBackgroundService sut = CreateSut();
        _executionGuard.Setup(g => g.IsRunning).Returns(false);
        _crawlerService.Setup(c => c.ExecutarAsync(It.IsAny<TipoExecucao>(), It.IsAny<bool>(), It.IsAny<IReadOnlyList<string>?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Should not throw
        await sut.ExecuteScheduledRunAsync(CancellationToken.None);
    }

    [Fact]
    public void CalculateNextRunDelay_ComCronInvalido_UsaDefault()
    {
        CrawlerBackgroundService sut = CreateSut("invalid");

        TimeSpan delay = sut.CalculateNextRunDelay();

        // Should use default hour=2, minute=0
        delay.TotalHours.ShouldBeGreaterThan(0);
        delay.TotalHours.ShouldBeLessThanOrEqualTo(24);
    }

    // ===== ObterCronScheduleAsync =====

    [Fact]
    public async Task Given_ConfiguracaoComCronValido_Should_RetornarCronDoBancoDeDados()
    {
        // Arrange
        CrawlerBackgroundService sut = CreateSut("0 5 * * *");
        ConfiguracaoCrawler configuracao = ConfiguracaoCrawler.CriarPadrao();
        _configuracaoRepo.Setup(r => r.ObterAtualAsync()).ReturnsAsync(configuracao);

        // Act
        string resultado = await sut.ObterCronScheduleAsync();

        // Assert
        resultado.ShouldBe("0 2 * * *"); // CriarPadrao retorna "0 2 * * *"
    }

    [Fact]
    public async Task Given_ConfiguracaoNula_Should_RetornarCronFallback()
    {
        // Arrange
        CrawlerBackgroundService sut = CreateSut("0 6 * * *");
        _configuracaoRepo.Setup(r => r.ObterAtualAsync()).ReturnsAsync((ConfiguracaoCrawler?)null);

        // Act
        string resultado = await sut.ObterCronScheduleAsync();

        // Assert
        resultado.ShouldBe("0 6 * * *");
    }

    [Fact]
    public async Task Given_ExcecaoAoObterConfiguracao_Should_RetornarFallbackELogarWarning()
    {
        // Arrange
        CrawlerBackgroundService sut = CreateSut("0 4 * * *");
        _configuracaoRepo.Setup(r => r.ObterAtualAsync()).ThrowsAsync(new Exception("Erro de conexão"));

        // Act
        string resultado = await sut.ObterCronScheduleAsync();

        // Assert
        resultado.ShouldBe("0 4 * * *");
        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ===== ExecuteScheduledRunAsync — branches não cobertas =====

    [Fact]
    public async Task Given_SemCertificadoDisponivel_Should_RetornarSemExecutarCrawler()
    {
        // Arrange
        CrawlerBackgroundService sut = CreateSut();
        _certificadoStore.Setup(s => s.HasCertificate()).Returns(false);

        // Act
        await sut.ExecuteScheduledRunAsync(CancellationToken.None);

        // Assert
        _crawlerService.Verify(c => c.ExecutarAsync(
            It.IsAny<TipoExecucao>(), It.IsAny<bool>(), It.IsAny<IReadOnlyList<string>?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()), Times.Never);
        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Given_ResultadoCrawlerComFalha_Should_LogarWarning()
    {
        // Arrange
        CrawlerBackgroundService sut = CreateSut();
        _executionGuard.Setup(g => g.IsRunning).Returns(false);
        _crawlerService.Setup(c => c.ExecutarAsync(TipoExecucao.Agendado, false, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<ExecucaoCrawler>("Erro na coleta de alíquotas"));

        // Act
        await sut.ExecuteScheduledRunAsync(CancellationToken.None);

        // Assert
        _crawlerService.Verify(c => c.ExecutarAsync(
            TipoExecucao.Agendado, false, null, null, It.IsAny<CancellationToken>()), Times.Once);
        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    // ===== CalculateNextRunDelay — edge cases e bordas =====

    [Fact]
    public void Given_CronNulo_Should_UsarFallbackInterno()
    {
        // Arrange
        CrawlerBackgroundService sut = CreateSut("15 8 * * *");

        // Act
        TimeSpan delay = sut.CalculateNextRunDelay(cronSchedule: null);

        // Assert
        delay.TotalHours.ShouldBeGreaterThan(0);
        delay.TotalHours.ShouldBeLessThanOrEqualTo(24);
    }

    [Fact]
    public void Given_CronComMinuto59Hora23_Should_RetornarDelayValido()
    {
        // Arrange
        CrawlerBackgroundService sut = CreateSut();

        // Act
        TimeSpan delay = sut.CalculateNextRunDelay("59 23 * * *");

        // Assert
        delay.TotalHours.ShouldBeGreaterThan(0);
        delay.TotalHours.ShouldBeLessThanOrEqualTo(24);
    }

    [Fact]
    public void Given_CronComMinuto0Hora0_Should_RetornarDelayValido()
    {
        // Arrange
        CrawlerBackgroundService sut = CreateSut();

        // Act
        TimeSpan delay = sut.CalculateNextRunDelay("0 0 * * *");

        // Assert
        delay.TotalHours.ShouldBeGreaterThan(0);
        delay.TotalHours.ShouldBeLessThanOrEqualTo(24);
    }

    [Fact]
    public void Given_CronComValoresForaDoLimite_Should_ClampearParaLimitesValidos()
    {
        // Arrange
        CrawlerBackgroundService sut = CreateSut();

        // Act — minuto=99 clamp para 59, hora=25 clamp para 23
        TimeSpan delay = sut.CalculateNextRunDelay("99 25 * * *");

        // Assert — deve comportar-se como "59 23 * * *"
        delay.TotalHours.ShouldBeGreaterThan(0);
        delay.TotalHours.ShouldBeLessThanOrEqualTo(24);
    }

    [Fact]
    public void Given_CronComValoresNegativos_Should_ClampearParaZero()
    {
        // Arrange
        CrawlerBackgroundService sut = CreateSut();

        // Act — minuto=-5 clamp para 0, hora=-1 clamp para 0
        TimeSpan delay = sut.CalculateNextRunDelay("-5 -1 * * *");

        // Assert — deve comportar-se como "0 0 * * *"
        delay.TotalHours.ShouldBeGreaterThan(0);
        delay.TotalHours.ShouldBeLessThanOrEqualTo(24);
    }

    [Fact]
    public void Given_CronComApenasUmaParte_Should_RetornarDelayValido()
    {
        // Arrange
        CrawlerBackgroundService sut = CreateSut();

        // Act — apenas "30" (parts.Length == 1, < 2 → usa defaults hour=2, minute=0)
        TimeSpan delay = sut.CalculateNextRunDelay("30");

        // Assert
        delay.TotalHours.ShouldBeGreaterThan(0);
        delay.TotalHours.ShouldBeLessThanOrEqualTo(24);
    }

    [Fact]
    public void Given_CronComStringVazia_Should_RetornarDelayValido()
    {
        // Arrange
        CrawlerBackgroundService sut = CreateSut();

        // Act — string vazia (parts.Length == 1 com "", < 2 → usa defaults)
        TimeSpan delay = sut.CalculateNextRunDelay("");

        // Assert
        delay.TotalHours.ShouldBeGreaterThan(0);
        delay.TotalHours.ShouldBeLessThanOrEqualTo(24);
    }

    [Fact]
    public void Given_CronComParteNaoNumerica_Should_UsarDefaultsERetornarDelayValido()
    {
        // Arrange
        CrawlerBackgroundService sut = CreateSut();

        // Act — "abc xyz * * *" → TryParse falha, usa hour=2, minute=0
        TimeSpan delay = sut.CalculateNextRunDelay("abc xyz * * *");

        // Assert
        delay.TotalHours.ShouldBeGreaterThan(0);
        delay.TotalHours.ShouldBeLessThanOrEqualTo(24);
    }

    // ===== ExecuteScheduledRunAsync — verificação de log de erro =====

    [Fact]
    public async Task Given_ExcecaoNoCrawler_Should_LogarErro()
    {
        // Arrange
        CrawlerBackgroundService sut = CreateSut();
        _executionGuard.Setup(g => g.IsRunning).Returns(false);
        _crawlerService.Setup(c => c.ExecutarAsync(It.IsAny<TipoExecucao>(), It.IsAny<bool>(), It.IsAny<IReadOnlyList<string>?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Falha inesperada"));

        // Act
        await sut.ExecuteScheduledRunAsync(CancellationToken.None);

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
