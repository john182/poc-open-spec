namespace MapaTributario.API.Application.Crawler;

public interface ICertificateProtection
{
    Task OnItemProcessedAsync(CancellationToken cancellationToken = default);
    void OnResponseReceived(int statusCode, double latencySeconds);
    bool ShouldHalt { get; }
    bool BudgetExhausted { get; }
    void Reset();
    int ProcessedCount { get; }
    int DailyRequestCount { get; }
}
