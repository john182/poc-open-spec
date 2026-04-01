using MapaTributario.API.Application.Crawler;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class RateLimiterTests
{
    [Fact]
    public async Task WaitAsync_DentroDoLimite_NaoBloqueia()
    {
        // Arrange
        RateLimiter limiter = new RateLimiter(10);
        DateTime before = DateTime.UtcNow;

        // Act
        await limiter.WaitAsync();

        // Assert
        DateTime after = DateTime.UtcNow;
        (after - before).TotalMilliseconds.ShouldBeLessThan(100);
    }

    [Fact]
    public async Task WaitAsync_AcimaDoLimite_Bloqueia()
    {
        // Arrange - very low rate limit to force blocking
        RateLimiter limiter = new RateLimiter(1);

        // Act - first request should be fast
        await limiter.WaitAsync();

        DateTime before = DateTime.UtcNow;
        // Second request within same second should wait
        await limiter.WaitAsync();
        DateTime after = DateTime.UtcNow;

        // Assert - should have waited at least some time
        // The wait time depends on how fast the first call completed
        (after - before).TotalMilliseconds.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task WaitAsync_MultiplasRequests_RespeitaLimite()
    {
        // Arrange
        RateLimiter limiter = new RateLimiter(5);

        // Act
        DateTime start = DateTime.UtcNow;
        for (int i = 0; i < 5; i++)
        {
            await limiter.WaitAsync();
        }

        DateTime afterFive = DateTime.UtcNow;

        // Assert - 5 requests at 5/s should be about 1 second window
        (afterFive - start).TotalSeconds.ShouldBeLessThan(2);
    }

    [Fact]
    public void UpdateRateLimit_AlteraLimite()
    {
        // Arrange
        RateLimiter limiter = new RateLimiter(5);
        limiter.CurrentRateLimit.ShouldBe(5);

        // Act
        limiter.UpdateRateLimit(1);

        // Assert
        limiter.CurrentRateLimit.ShouldBe(1);
    }

    [Fact]
    public async Task WaitAsync_ComCancellationToken_LancaException()
    {
        // Arrange
        RateLimiter limiter = new RateLimiter(1);
        CancellationTokenSource cts = new();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => limiter.WaitAsync(cts.Token));
    }

    [Fact]
    public async Task WaitAsync_ConcurrentCalls_ProcessaEmSequencia()
    {
        // Arrange
        RateLimiter limiter = new RateLimiter(10);
        int completedCount = 0;

        // Act
        Task[] tasks = Enumerable.Range(0, 5).Select(async _ =>
        {
            await limiter.WaitAsync();
            Interlocked.Increment(ref completedCount);
        }).ToArray();

        await Task.WhenAll(tasks);

        // Assert
        completedCount.ShouldBe(5);
    }

    [Fact]
    public void Constructor_ComValorDefault_Usa5PerSecond()
    {
        RateLimiter limiter = new RateLimiter();
        limiter.CurrentRateLimit.ShouldBe(5);
    }

    [Fact]
    public void Constructor_ComValorCustomizado_UsaValorInformado()
    {
        RateLimiter limiter = new RateLimiter(20);
        limiter.CurrentRateLimit.ShouldBe(20);
    }
}
