using MapaTributario.API.Application.Consulta;
using MapaTributario.API.Domain.Interfaces;
using MapaTributario.API.Infrastructure.Repository;
using MapaTributario.API.Infrastructure.Seed;

namespace MapaTributario.API.Extensions;

public static class ConsultaServiceExtensions
{
    public static IServiceCollection AddConsultaServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Repositories
        services.AddSingleton<IEstadoRepository, EstadoRepository>();
        services.AddSingleton<IMunicipioRepository, MunicipioRepository>();
        services.AddSingleton<IServicoRepository, ServicoRepository>();
        services.AddSingleton<IAliquotaRepository, AliquotaRepository>();

        // Application services
        services.AddScoped<IConsultaService, ConsultaService>();

        // Seed services
        services.AddTransient<IbgeSeedService>();
        services.AddTransient<ServicoSeedService>();

        return services;
    }

    public static async Task RunConsultaSeedsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var ibgeSeed = scope.ServiceProvider.GetRequiredService<IbgeSeedService>();
        await ibgeSeed.SeedAsync();

        var servicoSeed = scope.ServiceProvider.GetRequiredService<ServicoSeedService>();
        await servicoSeed.SeedAsync();
    }
}
