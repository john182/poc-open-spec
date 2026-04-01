using System.Security.Cryptography.X509Certificates;
using MapaTributario.API.Application.Crawler;
using MapaTributario.API.Domain.Interfaces;
using MapaTributario.API.Infrastructure;
using MapaTributario.API.Infrastructure.Auth;
using MapaTributario.API.Infrastructure.External;
using MapaTributario.API.Infrastructure.Repository;
using MapaTributario.API.Infrastructure.Repository.Mongo;
using MapaTributario.API.Infrastructure.Seed;
using MongoDB.Driver;

namespace MapaTributario.API.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddMapaTributarioInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // MongoDB
        MongoMappings.Register();
        CrawlerMongoMappings.Register();

        var mongoUri = configuration["MONGO_URI"]
            ?? configuration.GetConnectionString("MongoDB")
            ?? "mongodb://localhost:27017";
        var mongoDb = configuration["MONGO_DB"] ?? "mapa_tributario";
        services.AddSingleton<IMongoDatabase>(
            new MongoClient(mongoUri).GetDatabase(mongoDb));

        // Repositories — all Singleton (IMongoCollection<T> is thread-safe)
        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<IEstadoRepository, EstadoRepository>();
        services.AddSingleton<IMunicipioRepository, MunicipioRepository>();
        services.AddSingleton<IServicoRepository, ServicoRepository>();
        services.AddSingleton<IAliquotaRepository, AliquotaRepository>();
        services.AddSingleton<IExecucaoCrawlerRepository, ExecucaoCrawlerRepository>();
        services.AddSingleton<IFilaProcessamentoRepository, FilaProcessamentoRepository>();

        // Auth infrastructure
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ITokenProvider, JwtTokenProvider>();

        // Certificate store (Singleton — manages PFX in-memory)
        services.AddSingleton<ICertificadoStore, CertificadoStore>();

        // Seed services
        services.AddTransient<IbgeSeedService>();
        services.AddTransient<ServicoSeedService>();
        services.AddTransient<AdminSeedService>();

        // NFS-e API Client with certificate support
        RegisterNfseApiClient(services, configuration);

        return services;
    }

    public static async Task RunSeedsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var ibgeSeed = scope.ServiceProvider.GetRequiredService<IbgeSeedService>();
        await ibgeSeed.SeedAsync();

        var servicoSeed = scope.ServiceProvider.GetRequiredService<ServicoSeedService>();
        await servicoSeed.SeedAsync();

        var adminSeed = scope.ServiceProvider.GetRequiredService<AdminSeedService>();
        await adminSeed.SeedAsync();
    }

    public static async Task ApplyMongoIndexesAsync(this WebApplication app)
    {
        var database = app.Services.GetRequiredService<IMongoDatabase>();
        var indexSetup = new MongoIndexSetup(database);
        await indexSetup.ApplyAsync();
    }

    private static void RegisterNfseApiClient(
        IServiceCollection services,
        IConfiguration configuration)
    {
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
    }
}
