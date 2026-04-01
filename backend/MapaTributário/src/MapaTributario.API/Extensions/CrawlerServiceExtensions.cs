using System.Security.Cryptography.X509Certificates;
using MapaTributario.API.Application.Crawler;
using MapaTributario.API.Domain.Interfaces;
using MapaTributario.API.Infrastructure;
using MapaTributario.API.Infrastructure.External;
using MapaTributario.API.Infrastructure.Repository;
using MapaTributario.API.Infrastructure.Repository.Mongo;
using MapaTributario.API.Worker;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MapaTributario.API.Extensions;

public static class CrawlerServiceExtensions
{
    public static IServiceCollection AddCrawlerServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register Mongo mappings for crawler entities
        CrawlerMongoMappings.Register();

        // Crawler-specific repositories
        services.AddSingleton<IExecucaoCrawlerRepository, ExecucaoCrawlerRepository>();
        services.AddSingleton<IFilaProcessamentoRepository, FilaProcessamentoRepository>();

        // Shared domain repositories needed by CrawlerService
        // TryAdd prevents duplicate registration when ConsultaServiceExtensions also registers these
        services.TryAddSingleton<IMunicipioRepository, MunicipioRepository>();
        services.TryAddSingleton<IServicoRepository, ServicoRepository>();
        services.TryAddSingleton<IAliquotaRepository, AliquotaRepository>();

        // Resilience components (singletons for shared state)
        services.AddSingleton<IRateLimiter>(sp =>
        {
            int rateLimit = configuration.GetValue("Crawler:RateLimitPerSecond", 5);
            return new RateLimiter(rateLimit);
        });

        services.AddSingleton<ICircuitBreaker>(sp =>
        {
            ILogger<CircuitBreaker> logger = sp.GetRequiredService<ILogger<CircuitBreaker>>();
            int threshold = configuration.GetValue("Crawler:CircuitBreaker:ErrorThresholdPercent", 50);
            int window = configuration.GetValue("Crawler:CircuitBreaker:EvaluationWindowSeconds", 60);
            int pause = configuration.GetValue("Crawler:CircuitBreaker:PauseDurationSeconds", 300);
            int minSamples = configuration.GetValue("Crawler:CircuitBreaker:MinimumSamples", 10);
            return new CircuitBreaker(logger, threshold, window, pause, minSamples);
        });

        services.AddSingleton<ICertificateProtection>(sp =>
        {
            ILogger<CertificateProtection> logger = sp.GetRequiredService<ILogger<CertificateProtection>>();
            IRateLimiter rateLimiter = sp.GetRequiredService<IRateLimiter>();
            int batchSize = configuration.GetValue("Crawler:BatchSize", 50);
            int batchPause = configuration.GetValue("Crawler:BatchPauseSeconds", 30);
            int dailyBudget = configuration.GetValue("Crawler:DailyBudget", 50000);
            return new CertificateProtection(logger, rateLimiter, batchSize, batchPause, dailyBudget);
        });

        // Certificate store (Singleton — manages PFX in-memory)
        services.AddSingleton<ICertificadoStore, CertificadoStore>();

        // Crawler execution guard (Singleton — atomic concurrency control)
        services.AddSingleton<ICrawlerExecutionGuard, CrawlerExecutionGuard>();

        // NFS-e API Client
        NfseApiClientOptions nfseOptions = new NfseApiClientOptions();
        configuration.GetSection(NfseApiClientOptions.SectionName).Bind(nfseOptions);

        services.AddHttpClient<INfseApiClient, NfseApiClient>(client =>
        {
            client.BaseAddress = new Uri(nfseOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(nfseOptions.TimeoutSeconds);
        })
        .ConfigurePrimaryHttpMessageHandler(sp =>
        {
            ICertificadoStore certificadoStore = sp.GetRequiredService<ICertificadoStore>();
            HttpClientHandler handler = new HttpClientHandler();

            // Priority 1: certificate from ICertificadoStore (uploaded via API)
            X509Certificate2? dynamicCert = certificadoStore.GetCertificate();
            if (dynamicCert is not null)
            {
                handler.ClientCertificates.Add(dynamicCert);
            }
            // Priority 2: fallback to static file from appsettings
            else if (!string.IsNullOrEmpty(nfseOptions.CertificatePath) && File.Exists(nfseOptions.CertificatePath))
            {
                X509Certificate2 certificate = string.IsNullOrEmpty(nfseOptions.CertificatePassword)
                    ? X509CertificateLoader.LoadPkcs12FromFile(nfseOptions.CertificatePath, null)
                    : X509CertificateLoader.LoadPkcs12FromFile(nfseOptions.CertificatePath, nfseOptions.CertificatePassword);
                handler.ClientCertificates.Add(certificate);
            }

            return handler;
        });

        // Crawler service (Scoped — has scoped dependencies like repositories)
        services.AddScoped<ICrawlerService, CrawlerService>();

        // Background service
        services.AddHostedService<CrawlerBackgroundService>();

        return services;
    }
}
