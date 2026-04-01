using MapaTributario.API.Application.Crawler;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class CircuitBreakerTests
{
    private readonly Mock<ILogger<CircuitBreaker>> _logger = new();

    [Fact]
    public void Estado_Inicial_Fechado()
    {
        CircuitBreaker cb = new CircuitBreaker(_logger.Object);
        cb.State.ShouldBe(CircuitBreakerState.Closed);
        cb.IsOpen.ShouldBeFalse();
    }

    [Fact]
    public void RecordSuccess_MantemFechado()
    {
        CircuitBreaker cb = new CircuitBreaker(_logger.Object);
        cb.RecordSuccess();
        cb.State.ShouldBe(CircuitBreakerState.Closed);
    }

    [Fact]
    public void RecordFailure_AbaixoDoThreshold_MantemFechado()
    {
        // min 10 samples, threshold 50%
        CircuitBreaker cb = new CircuitBreaker(_logger.Object, minimumSamples: 10);

        // 4 failures + 6 successes = 40% error rate
        for (int i = 0; i < 6; i++)
        {
            cb.RecordSuccess();
        }

        for (int i = 0; i < 4; i++)
        {
            cb.RecordFailure();
        }

        cb.State.ShouldBe(CircuitBreakerState.Closed);
    }

    [Fact]
    public void RecordFailure_AcimaDoThreshold_AbreCircuito()
    {
        CircuitBreaker cb = new CircuitBreaker(_logger.Object, errorThresholdPercent: 50, minimumSamples: 10);

        // 4 successes + 6 failures = 60% error rate
        for (int i = 0; i < 4; i++)
        {
            cb.RecordSuccess();
        }

        for (int i = 0; i < 6; i++)
        {
            cb.RecordFailure();
        }

        cb.IsOpen.ShouldBeTrue();
    }

    [Fact]
    public void RecordFailure_ComMenosQueMinimumSamples_NaoAbreCircuito()
    {
        CircuitBreaker cb = new CircuitBreaker(_logger.Object, minimumSamples: 10);

        // 5 failures (below minimum samples of 10)
        for (int i = 0; i < 5; i++)
        {
            cb.RecordFailure();
        }

        cb.State.ShouldBe(CircuitBreakerState.Closed);
    }

    [Fact]
    public async Task Estado_AposAberto_TransitaParaSemiAberto()
    {
        // Very short pause so we can test transition
        CircuitBreaker cb = new CircuitBreaker(_logger.Object,
            errorThresholdPercent: 50,
            minimumSamples: 2,
            pauseDurationSeconds: 0); // 0 seconds for immediate transition

        // Open the circuit
        cb.RecordFailure();
        cb.RecordFailure();
        cb.IsOpen.ShouldBeTrue();

        // Should transition to HalfOpen immediately since pauseDuration is 0
        // Need to wait a tiny bit for the transition check
        await Task.Delay(10);
        cb.State.ShouldBe(CircuitBreakerState.HalfOpen);
    }

    [Fact]
    public async Task RecordSuccess_EmHalfOpen_FechaCircuito()
    {
        CircuitBreaker cb = new CircuitBreaker(_logger.Object,
            errorThresholdPercent: 50,
            minimumSamples: 2,
            pauseDurationSeconds: 0);

        // Open the circuit
        cb.RecordFailure();
        cb.RecordFailure();

        // Wait for HalfOpen transition
        await Task.Delay(10);
        cb.State.ShouldBe(CircuitBreakerState.HalfOpen);

        // Probe success
        cb.RecordSuccess();
        cb.State.ShouldBe(CircuitBreakerState.Closed);
    }

    [Fact]
    public async Task RecordFailure_EmHalfOpen_ReabreCircuito()
    {
        CircuitBreaker cb = new CircuitBreaker(_logger.Object,
            errorThresholdPercent: 50,
            minimumSamples: 2,
            pauseDurationSeconds: 0);

        // Open the circuit
        cb.RecordFailure();
        cb.RecordFailure();

        // Wait for HalfOpen transition
        await Task.Delay(10);
        cb.State.ShouldBe(CircuitBreakerState.HalfOpen);

        // Probe failure
        cb.RecordFailure();
        cb.IsOpen.ShouldBeTrue();
    }

    [Fact]
    public void Reset_LimpaEstado()
    {
        CircuitBreaker cb = new CircuitBreaker(_logger.Object, minimumSamples: 2);

        cb.RecordFailure();
        cb.RecordFailure();
        cb.IsOpen.ShouldBeTrue();

        cb.Reset();
        cb.State.ShouldBe(CircuitBreakerState.Closed);
        cb.IsOpen.ShouldBeFalse();
    }

    [Fact]
    public async Task WaitIfOpenAsync_QuandoFechado_RetornaImediatamente()
    {
        CircuitBreaker cb = new CircuitBreaker(_logger.Object);
        DateTime before = DateTime.UtcNow;

        await cb.WaitIfOpenAsync();

        DateTime after = DateTime.UtcNow;
        (after - before).TotalMilliseconds.ShouldBeLessThan(100);
    }

    [Fact]
    public async Task WaitIfOpenAsync_QuandoSemiAberto_RetornaImediatamente()
    {
        CircuitBreaker cb = new CircuitBreaker(_logger.Object,
            minimumSamples: 2, pauseDurationSeconds: 0);

        cb.RecordFailure();
        cb.RecordFailure();
        await Task.Delay(10);

        DateTime before = DateTime.UtcNow;
        await cb.WaitIfOpenAsync();
        DateTime after = DateTime.UtcNow;

        (after - before).TotalMilliseconds.ShouldBeLessThan(100);
    }
}
