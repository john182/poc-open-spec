using Microsoft.Extensions.Logging;

namespace MapaTributario.API.Application.Crawler;

public class CircuitBreaker : ICircuitBreaker
{
    private readonly int _errorThresholdPercent;
    private readonly TimeSpan _evaluationWindow;
    private readonly TimeSpan _pauseDuration;
    private readonly int _minimumSamples;
    private readonly ILogger<CircuitBreaker> _logger;

    private readonly List<(DateTime Timestamp, bool IsSuccess)> _records = new();
    private readonly object _lock = new();

    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private DateTime? _openedAt;

    public CircuitBreaker(
        ILogger<CircuitBreaker> logger,
        int errorThresholdPercent = 50,
        int evaluationWindowSeconds = 60,
        int pauseDurationSeconds = 300,
        int minimumSamples = 10)
    {
        _logger = logger;
        _errorThresholdPercent = errorThresholdPercent;
        _evaluationWindow = TimeSpan.FromSeconds(evaluationWindowSeconds);
        _pauseDuration = TimeSpan.FromSeconds(pauseDurationSeconds);
        _minimumSamples = minimumSamples;
    }

    public bool IsOpen => _state == CircuitBreakerState.Open;

    public CircuitBreakerState State
    {
        get
        {
            lock (_lock)
            {
                if (_state == CircuitBreakerState.Open && _openedAt.HasValue)
                {
                    if (DateTime.UtcNow - _openedAt.Value >= _pauseDuration)
                    {
                        _state = CircuitBreakerState.HalfOpen;
                        _logger.LogInformation("Circuit breaker transitioned to HalfOpen for probe");
                    }
                }

                return _state;
            }
        }
    }

    public void RecordSuccess()
    {
        lock (_lock)
        {
            if (_state == CircuitBreakerState.HalfOpen)
            {
                _state = CircuitBreakerState.Closed;
                _records.Clear();
                _openedAt = null;
                _logger.LogInformation("Circuit breaker closed after successful probe");
                return;
            }

            PruneOldRecords();
            _records.Add((DateTime.UtcNow, true));
        }
    }

    public void RecordFailure()
    {
        lock (_lock)
        {
            if (_state == CircuitBreakerState.HalfOpen)
            {
                _state = CircuitBreakerState.Open;
                _openedAt = DateTime.UtcNow;
                _logger.LogWarning("Circuit breaker re-opened after failed probe");
                return;
            }

            PruneOldRecords();
            _records.Add((DateTime.UtcNow, false));
            EvaluateThreshold();
        }
    }

    public async Task WaitIfOpenAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            CircuitBreakerState currentState = State;

            if (currentState == CircuitBreakerState.Closed || currentState == CircuitBreakerState.HalfOpen)
            {
                return;
            }

            _logger.LogWarning("Circuit breaker is open. Waiting {PauseDuration}s before probe",
                _pauseDuration.TotalSeconds);

            await Task.Delay(_pauseDuration, cancellationToken);
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _state = CircuitBreakerState.Closed;
            _records.Clear();
            _openedAt = null;
        }
    }

    private void PruneOldRecords()
    {
        DateTime cutoff = DateTime.UtcNow - _evaluationWindow;
        _records.RemoveAll(r => r.Timestamp < cutoff);
    }

    private void EvaluateThreshold()
    {
        if (_records.Count < _minimumSamples)
        {
            return;
        }

        int failures = _records.Count(r => !r.IsSuccess);
        double errorRate = (double)failures / _records.Count * 100;

        if (errorRate > _errorThresholdPercent)
        {
            _state = CircuitBreakerState.Open;
            _openedAt = DateTime.UtcNow;
            _logger.LogWarning(
                "Circuit breaker OPENED. Error rate: {ErrorRate:F1}% ({Failures}/{Total})",
                errorRate, failures, _records.Count);
        }
    }
}
