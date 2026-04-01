using Microsoft.Extensions.Logging;

namespace MapaTributario.API.Application.Crawler;

public class CertificateProtection : ICertificateProtection
{
    private readonly ILogger<CertificateProtection> _logger;
    private readonly IRateLimiter _rateLimiter;

    private readonly int _batchSize;
    private readonly int _batchPauseSeconds;
    private readonly int _dailyBudget;
    private readonly double _latencyThresholdSeconds;
    private readonly int _throttlePauseSeconds;

    private int _processedCount;
    private int _dailyRequestCount;
    private int _consecutive403Count;
    private bool _shouldHalt;
    private bool _isThrottled;
    private readonly Queue<double> _recentLatencies = new();
    private readonly object _lock = new();

    public CertificateProtection(
        ILogger<CertificateProtection> logger,
        IRateLimiter rateLimiter,
        int batchSize = 50,
        int batchPauseSeconds = 30,
        int dailyBudget = 50000,
        double latencyThresholdSeconds = 5.0,
        int throttlePauseSeconds = 120)
    {
        _logger = logger;
        _rateLimiter = rateLimiter;
        _batchSize = batchSize;
        _batchPauseSeconds = batchPauseSeconds;
        _dailyBudget = dailyBudget;
        _latencyThresholdSeconds = latencyThresholdSeconds;
        _throttlePauseSeconds = throttlePauseSeconds;
    }

    public bool ShouldHalt
    {
        get
        {
            lock (_lock)
            {
                return _shouldHalt;
            }
        }
    }

    public bool BudgetExhausted
    {
        get
        {
            lock (_lock)
            {
                return _dailyRequestCount >= _dailyBudget;
            }
        }
    }

    public int ProcessedCount
    {
        get
        {
            lock (_lock)
            {
                return _processedCount;
            }
        }
    }

    public int DailyRequestCount
    {
        get
        {
            lock (_lock)
            {
                return _dailyRequestCount;
            }
        }
    }

    public async Task OnItemProcessedAsync(CancellationToken cancellationToken = default)
    {
        int currentCount;
        bool budgetExhausted;

        lock (_lock)
        {
            _processedCount++;
            _dailyRequestCount++;
            currentCount = _processedCount;
            budgetExhausted = _dailyRequestCount >= _dailyBudget;
        }

        if (budgetExhausted)
        {
            _logger.LogWarning(
                "Daily request budget exhausted ({Budget}). Stopping processing",
                _dailyBudget);
            return;
        }

        if (currentCount % _batchSize == 0)
        {
            _logger.LogInformation(
                "Batch pause after {Count} items. Pausing {Seconds}s",
                currentCount, _batchPauseSeconds);
            await Task.Delay(TimeSpan.FromSeconds(_batchPauseSeconds), cancellationToken);
        }
    }

    public void OnResponseReceived(int statusCode, double latencySeconds)
    {
        lock (_lock)
        {
            // Track latency
            _recentLatencies.Enqueue(latencySeconds);
            while (_recentLatencies.Count > 20)
            {
                _recentLatencies.Dequeue();
            }

            // 403 consecutive tracking
            if (statusCode == 403)
            {
                _consecutive403Count++;
                if (_consecutive403Count >= 3)
                {
                    _shouldHalt = true;
                    _logger.LogCritical(
                        "HALT: 3 consecutive 403 responses detected. Certificate may be blocked");
                    return;
                }
            }
            else
            {
                _consecutive403Count = 0;
            }

            // Adaptive throttling: 429, unexpected 403, or high latency
            bool shouldThrottle = statusCode == 429
                || (statusCode == 403 && _consecutive403Count > 0)
                || IsHighLatency();

            if (shouldThrottle && !_isThrottled)
            {
                _isThrottled = true;
                _rateLimiter.UpdateRateLimit(1);
                _logger.LogWarning(
                    "Adaptive throttling activated. Rate reduced to 1 req/s. " +
                    "StatusCode={StatusCode}, AvgLatency={AvgLatency:F2}s",
                    statusCode,
                    _recentLatencies.Count > 0 ? _recentLatencies.Average() : 0);
            }
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _processedCount = 0;
            _consecutive403Count = 0;
            _shouldHalt = false;
            _isThrottled = false;
            _recentLatencies.Clear();
        }
    }

    private bool IsHighLatency()
    {
        if (_recentLatencies.Count < 20)
        {
            return false;
        }

        double average = _recentLatencies.Average();
        return average > _latencyThresholdSeconds;
    }
}
