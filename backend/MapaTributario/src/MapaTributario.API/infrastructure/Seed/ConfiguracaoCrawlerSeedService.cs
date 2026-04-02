using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;

namespace MapaTributario.API.Infrastructure.Seed;

public class ConfiguracaoCrawlerSeedService
{
    private readonly IConfiguracaoCrawlerRepository _repository;
    private readonly ILogger<ConfiguracaoCrawlerSeedService> _logger;

    public ConfiguracaoCrawlerSeedService(
        IConfiguracaoCrawlerRepository repository,
        ILogger<ConfiguracaoCrawlerSeedService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        bool existe = await _repository.ExisteAlgumaAsync();
        if (existe)
        {
            _logger.LogInformation(
                "Configuração de crawler já existe. Seed ignorado.");
            return;
        }

        ConfiguracaoCrawler configuracao = ConfiguracaoCrawler.CriarPadrao();
        await _repository.CriarAsync(configuracao);

        _logger.LogInformation(
            "Seed de configuração do crawler concluído com valores padrão.");
    }
}
