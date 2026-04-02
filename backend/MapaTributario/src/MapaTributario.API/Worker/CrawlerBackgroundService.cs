using FluentResults;
using MapaTributario.API.Application.Crawler;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;

namespace MapaTributario.API.Worker;

public class CrawlerBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICrawlerExecutionGuard _executionGuard;
    private readonly ICertificadoStore _certificadoStore;
    private readonly IConfiguracaoCrawlerRepository _configuracaoRepository;
    private readonly ILogger<CrawlerBackgroundService> _logger;
    private readonly string _cronScheduleFallback;

    public CrawlerBackgroundService(
        IServiceProvider serviceProvider,
        ICrawlerExecutionGuard executionGuard,
        ICertificadoStore certificadoStore,
        IConfiguracaoCrawlerRepository configuracaoRepository,
        ILogger<CrawlerBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _executionGuard = executionGuard;
        _certificadoStore = certificadoStore;
        _configuracaoRepository = configuracaoRepository;
        _logger = logger;
        _cronScheduleFallback = configuration["Crawler:CronSchedule"] ?? "0 2 * * *";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string cronSchedule = await ObterCronScheduleAsync();
        _logger.LogInformation("CrawlerBackgroundService started with schedule: {Schedule}", cronSchedule);

        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan delay = CalculateNextRunDelay(cronSchedule);
            _logger.LogInformation("Next crawler execution scheduled in {Delay}", delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            // Recarregar CronSchedule antes de cada execução (pode ter mudado via admin)
            cronSchedule = await ObterCronScheduleAsync();

            await ExecuteScheduledRunAsync(stoppingToken);
        }

        _logger.LogInformation("CrawlerBackgroundService is stopping");
    }

    internal async Task<string> ObterCronScheduleAsync()
    {
        try
        {
            ConfiguracaoCrawler? configuracao = await _configuracaoRepository.ObterAtivaAsync();
            return configuracao?.CronSchedule ?? _cronScheduleFallback;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Falha ao obter CronSchedule do MongoDB. Usando fallback: {Fallback}",
                _cronScheduleFallback);
            return _cronScheduleFallback;
        }
    }

    internal async Task ExecuteScheduledRunAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting scheduled crawler execution");

        try
        {
            if (!_certificadoStore.HasCertificate())
            {
                _logger.LogWarning("Execução agendada ignorada: nenhum certificado digital disponível");
                return;
            }

            if (_executionGuard.IsRunning)
            {
                _logger.LogWarning("Crawler is already running. Skipping scheduled execution");
                return;
            }

            using IServiceScope scope = _serviceProvider.CreateScope();
            ICrawlerService crawlerService = scope.ServiceProvider.GetRequiredService<ICrawlerService>();

            var resultado = await crawlerService.ExecutarAsync(TipoExecucao.Agendado, cancellationToken: stoppingToken);
            if (resultado.IsFailed)
            {
                _logger.LogWarning("Scheduled crawler execution returned failure: {Errors}",
                    string.Join("; ", resultado.Errors.Select(e => e.Message)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scheduled crawler execution failed");
        }
    }

    internal TimeSpan CalculateNextRunDelay(string? cronSchedule = null)
    {
        string schedule = cronSchedule ?? _cronScheduleFallback;
        // Parse simple daily CRON: "0 H * * *"
        // Default: 02:00 UTC
        int hour = 2;
        int minute = 0;

        string[] parts = schedule.Split(' ');
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
