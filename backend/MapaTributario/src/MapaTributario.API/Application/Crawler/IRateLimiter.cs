namespace MapaTributario.API.Application.Crawler;

public interface IRateLimiter
{
    Task WaitAsync(CancellationToken cancellationToken = default);
    void UpdateRateLimit(int requestsPerSecond);
    int CurrentRateLimit { get; }
}
