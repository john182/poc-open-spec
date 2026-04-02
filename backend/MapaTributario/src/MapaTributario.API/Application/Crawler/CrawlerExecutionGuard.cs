namespace MapaTributario.API.Application.Crawler;

public class CrawlerExecutionGuard : ICrawlerExecutionGuard
{
    private int _running;

    public bool IsRunning => Interlocked.CompareExchange(ref _running, 0, 0) == 1;

    public bool TryAcquire()
    {
        return Interlocked.CompareExchange(ref _running, 1, 0) == 0;
    }

    public void Release()
    {
        Interlocked.Exchange(ref _running, 0);
    }
}
