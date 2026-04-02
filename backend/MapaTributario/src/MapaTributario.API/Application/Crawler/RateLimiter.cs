namespace MapaTributario.API.Application.Crawler;

public class RateLimiter : IRateLimiter
{
    private readonly SemaphoreSlim _semaphore;
    private int _requestsPerSecond;
    private readonly object _lock = new();
    private DateTime _windowStart;
    private int _requestsInWindow;

    public RateLimiter(int requestsPerSecond = 5)
    {
        _requestsPerSecond = requestsPerSecond;
        _semaphore = new SemaphoreSlim(1, 1);
        _windowStart = DateTime.UtcNow;
        _requestsInWindow = 0;
    }

    public int CurrentRateLimit => _requestsPerSecond;

    public async Task WaitAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            DateTime now = DateTime.UtcNow;
            TimeSpan elapsed = now - _windowStart;

            if (elapsed.TotalSeconds >= 1.0)
            {
                _windowStart = now;
                _requestsInWindow = 0;
            }

            if (_requestsInWindow >= _requestsPerSecond)
            {
                double waitMs = 1000.0 - elapsed.TotalMilliseconds;
                if (waitMs > 0)
                {
                    await Task.Delay((int)waitMs, cancellationToken);
                }

                _windowStart = DateTime.UtcNow;
                _requestsInWindow = 0;
            }

            _requestsInWindow++;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void UpdateRateLimit(int requestsPerSecond)
    {
        lock (_lock)
        {
            _requestsPerSecond = requestsPerSecond;
        }
    }
}
