using FluentResults;
using MapaTributario.API.Application.Crawler;
using MapaTributario.API.Domain.Entities;
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

        return new CrawlerBackgroundService(_serviceProvider.Object, _executionGuard.Object, _certificadoStore.Object, _logger.Object, configuration);
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
        _crawlerService.Setup(c => c.ExecutarAsync(TipoExecucao.Agendado, false, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(ExecucaoCrawler.Create(TipoExecucao.Agendado)));

        await sut.ExecuteScheduledRunAsync(CancellationToken.None);

        _crawlerService.Verify(c => c.ExecutarAsync(
            TipoExecucao.Agendado, false, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteScheduledRunAsync_QuandoCrawlerJaRodando_NaoExecuta()
    {
        CrawlerBackgroundService sut = CreateSut();
        _executionGuard.Setup(g => g.IsRunning).Returns(true);

        await sut.ExecuteScheduledRunAsync(CancellationToken.None);

        _crawlerService.Verify(c => c.ExecutarAsync(
            It.IsAny<TipoExecucao>(), It.IsAny<bool>(), It.IsAny<IReadOnlyList<string>?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteScheduledRunAsync_ComExcecao_NaoLancaException()
    {
        CrawlerBackgroundService sut = CreateSut();
        _executionGuard.Setup(g => g.IsRunning).Returns(false);
        _crawlerService.Setup(c => c.ExecutarAsync(It.IsAny<TipoExecucao>(), It.IsAny<bool>(), It.IsAny<IReadOnlyList<string>?>(), It.IsAny<CancellationToken>()))
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
}
