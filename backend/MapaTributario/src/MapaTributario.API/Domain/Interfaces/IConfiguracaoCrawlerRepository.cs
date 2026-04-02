using MapaTributario.API.Domain.Entities;

namespace MapaTributario.API.Domain.Interfaces;

public interface IConfiguracaoCrawlerRepository
{
    Task<ConfiguracaoCrawler?> ObterAtualAsync();
    Task<bool> ExisteAlgumaAsync();
    Task<ConfiguracaoCrawler> CriarAsync(ConfiguracaoCrawler configuracao);
    Task AtualizarAsync(ConfiguracaoCrawler configuracao);
}
