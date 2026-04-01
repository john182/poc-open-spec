namespace MapaTributario.API.Application.Crawler;

public interface ICircuitBreaker
{
    bool IsOpen { get; }
    CircuitBreakerState State { get; }
    void RecordSuccess();
    void RecordFailure();
    Task WaitIfOpenAsync(CancellationToken cancellationToken = default);
    void Reset();
}

public enum CircuitBreakerState
{
    Closed,
    Open,
    HalfOpen
}
