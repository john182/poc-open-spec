using System.Text;
using FluentValidation;
using MapaTributario.API.Application.Auth;
using MapaTributario.API.Application.Consulta;
using MapaTributario.API.Application.Crawler;
using MapaTributario.API.Worker;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace MapaTributario.API.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddMapaTributarioApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Auth use cases
        services.AddScoped<RegisterUser>();
        services.AddScoped<LoginUser>();
        services.AddScoped<RefreshToken>();

        // Consulta
        services.AddScoped<IConsultaService, ConsultaService>();

        // Crawler
        services.AddScoped<ICrawlerService, CrawlerService>();
        services.AddSingleton<ICrawlerExecutionGuard, CrawlerExecutionGuard>();

        // Configuração Crawler (CRUD)
        services.AddScoped<IConfiguracaoCrawlerAppService, ConfiguracaoCrawlerAppService>();

        // Resilience components (singletons for shared state)
        // NOTA: Os parâmetros de RateLimiter, CircuitBreaker e CertificateProtection são lidos
        // apenas do appsettings.json na inicialização. Os campos correspondentes em
        // ConfiguracaoCrawler (ex: CircuitBreakerLimiarErroPercent, LimiteRequisicoesPorSegundo)
        // ainda NÃO são aplicados dinamicamente em runtime. Futuramente, esses singletons
        // deverão ser substituídos por instâncias que recarreguem os valores da configuração
        // MongoDB a cada execução do crawler.
        RegisterResilienceComponents(services, configuration);

        // Background service
        services.AddHostedService<CrawlerBackgroundService>();

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<Program>();

        // JWT Authentication
        RegisterJwtAuthentication(services, configuration);

        return services;
    }

    private static void RegisterResilienceComponents(
        IServiceCollection services,
        IConfiguration configuration)
    {
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
    }

    private static void RegisterJwtAuthentication(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSecret = configuration["JWT:Secret"]
            ?? configuration["JWT_SECRET"]
            ?? throw new InvalidOperationException(
                "JWT secret not configured. Set 'JWT:Secret' in appsettings or 'JWT_SECRET' environment variable.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["JWT:Issuer"] ?? "MapaTributario",
                    ValidateAudience = true,
                    ValidAudience = configuration["JWT:Audience"] ?? "MapaTributario",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();
    }
}
