using MapaTributario.API.Application.Crawler;
using MapaTributario.API.Domain.Entities;

namespace MapaTributario.API.Worker;

public class CrawlerBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICrawlerExecutionGuard _executionGuard;
    private readonly ILogger<CrawlerBackgroundService> _logger;
    private readonly string _cronSchedule;

    public CrawlerBackgroundService(
        IServiceProvider serviceProvider,
        ICrawlerExecutionGuard executionGuard,
        ILogger<CrawlerBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _executionGuard = executionGuard;
        _logger = logger;
        _cronSchedule = configuration["Crawler:CronSchedule"] ?? "0 2 * * *";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CrawlerBackgroundService started with schedule: {Schedule}", _cronSchedule);

        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan delay = CalculateNextRunDelay();
            _logger.LogInformation("Next crawler execution scheduled in {Delay}", delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await ExecuteScheduledRunAsync(stoppingToken);
        }

        _logger.LogInformation("CrawlerBackgroundService is stopping");
    }

    internal async Task ExecuteScheduledRunAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting scheduled crawler execution");

        try
        {
            if (_executionGuard.IsRunning)
            {
                _logger.LogWarning("Crawler is already running. Skipping scheduled execution");
                return;
            }

            using IServiceScope scope = _serviceProvider.CreateScope();
            ICrawlerService crawlerService = scope.ServiceProvider.GetRequiredService<ICrawlerService>();

            await crawlerService.ExecutarAsync(TipoExecucao.Agendado, cancellationToken: stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scheduled crawler execution failed");
        }
    }

    internal TimeSpan CalculateNextRunDelay()
    {
        // Parse simple daily CRON: "0 H * * *"
        // Default: 02:00 UTC
        int hour = 2;
        int minute = 0;

        string[] parts = _cronSchedule.Split(' ');
        if (parts.Length >= 2)
        {
            if (int.TryParse(parts[0], out int parsedMinute))
            {
                minute = parsedMinute;
            }

            if (int.TryParse(parts[1], out int parsedHour))
            {
                hour = parsedHour;
            }
        }

        DateTime now = DateTime.UtcNow;
        DateTime nextRun = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0, DateTimeKind.Utc);

        if (nextRun <= now)
        {
            nextRun = nextRun.AddDays(1);
        }

        return nextRun - now;
    }
}
