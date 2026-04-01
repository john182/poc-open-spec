namespace MapaTributario.API.Application.Crawler;

public interface ICrawlerExecutionGuard
{
    bool TryAcquire();
    void Release();
    bool IsRunning { get; }
}
