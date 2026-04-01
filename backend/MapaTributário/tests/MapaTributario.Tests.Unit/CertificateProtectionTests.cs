using MapaTributario.API.Application.Crawler;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class CertificateProtectionTests
{
    private readonly Mock<ILogger<CertificateProtection>> _logger = new();
    private readonly Mock<IRateLimiter> _rateLimiter = new();

    private CertificateProtection CreateSut(
        int batchSize = 50,
        int batchPauseSeconds = 0,
        int dailyBudget = 50000)
    {
        return new CertificateProtection(
            _logger.Object,
            _rateLimiter.Object,
            batchSize,
            batchPauseSeconds,
            dailyBudget);
    }

    [Fact]
    public void Estado_Inicial_NaoHalt()
    {
        CertificateProtection sut = CreateSut();
        sut.ShouldHalt.ShouldBeFalse();
        sut.BudgetExhausted.ShouldBeFalse();
        sut.ProcessedCount.ShouldBe(0);
        sut.DailyRequestCount.ShouldBe(0);
    }

    [Fact]
    public async Task OnItemProcessedAsync_IncrementaContadores()
    {
        CertificateProtection sut = CreateSut();

        await sut.OnItemProcessedAsync();

        sut.ProcessedCount.ShouldBe(1);
        sut.DailyRequestCount.ShouldBe(1);
    }

    [Fact]
    public async Task OnItemProcessedAsync_ComBatchPause_PausaNoLimite()
    {
        // batchSize = 3, no delay for speed
        CertificateProtection sut = CreateSut(batchSize: 3, batchPauseSeconds: 0);

        for (int i = 0; i < 4; i++)
        {
            await sut.OnItemProcessedAsync();
        }

        sut.ProcessedCount.ShouldBe(4);
    }

    [Fact]
    public async Task OnItemProcessedAsync_ComBudgetExhausted_SinalizaBudgetExhausted()
    {
        CertificateProtection sut = CreateSut(dailyBudget: 3);

        for (int i = 0; i < 3; i++)
        {
            await sut.OnItemProcessedAsync();
        }

        sut.BudgetExhausted.ShouldBeTrue();
        sut.DailyRequestCount.ShouldBe(3);
    }

    [Fact]
    public void OnResponseReceived_Com3x403Consecutivos_AtivaModoHalt()
    {
        CertificateProtection sut = CreateSut();

        sut.OnResponseReceived(403, 0.5);
        sut.ShouldHalt.ShouldBeFalse();

        sut.OnResponseReceived(403, 0.5);
        sut.ShouldHalt.ShouldBeFalse();

        sut.OnResponseReceived(403, 0.5);
        sut.ShouldHalt.ShouldBeTrue();
    }

    [Fact]
    public void OnResponseReceived_Com403Intercalado200_NaoAtivaHalt()
    {
        CertificateProtection sut = CreateSut();

        sut.OnResponseReceived(403, 0.5);
        sut.OnResponseReceived(200, 0.5);
        sut.OnResponseReceived(403, 0.5);
        sut.OnResponseReceived(403, 0.5);

        sut.ShouldHalt.ShouldBeFalse();
    }

    [Fact]
    public void OnResponseReceived_Com429_AtivaThrottling()
    {
        CertificateProtection sut = CreateSut();

        sut.OnResponseReceived(429, 0.5);

        _rateLimiter.Verify(r => r.UpdateRateLimit(1), Times.Once);
    }

    [Fact]
    public void OnResponseReceived_ComLatenciaAlta_AtivaThrottling()
    {
        CertificateProtection sut = CreateSut();

        // Need 20 samples with high latency
        for (int i = 0; i < 20; i++)
        {
            sut.OnResponseReceived(200, 6.0); // above 5s threshold
        }

        _rateLimiter.Verify(r => r.UpdateRateLimit(1), Times.Once);
    }

    [Fact]
    public void OnResponseReceived_ComLatenciaBaixa_NaoAtivaThrottling()
    {
        CertificateProtection sut = CreateSut();

        for (int i = 0; i < 20; i++)
        {
            sut.OnResponseReceived(200, 0.5);
        }

        _rateLimiter.Verify(r => r.UpdateRateLimit(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void Reset_LimpaContadoresEFlags()
    {
        CertificateProtection sut = CreateSut();

        sut.OnResponseReceived(403, 0.5);
        sut.OnResponseReceived(403, 0.5);
        sut.OnResponseReceived(403, 0.5);
        sut.ShouldHalt.ShouldBeTrue();

        sut.Reset();

        sut.ShouldHalt.ShouldBeFalse();
        sut.ProcessedCount.ShouldBe(0);
    }

    [Fact]
    public async Task OnItemProcessedAsync_ComBudgetExauridoAntes_RetornaSemPausa()
    {
        CertificateProtection sut = CreateSut(dailyBudget: 1);

        await sut.OnItemProcessedAsync();
        sut.BudgetExhausted.ShouldBeTrue();

        // Second call should still work (just won't process)
        await sut.OnItemProcessedAsync();
        sut.DailyRequestCount.ShouldBe(2);
    }

    [Fact]
    public void OnResponseReceived_ThrottlingAtivadoApenas1Vez()
    {
        CertificateProtection sut = CreateSut();

        sut.OnResponseReceived(429, 0.5);
        sut.OnResponseReceived(429, 0.5);

        // Should only call UpdateRateLimit once (throttling already active)
        _rateLimiter.Verify(r => r.UpdateRateLimit(1), Times.Once);
    }
}
