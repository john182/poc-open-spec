using System.Diagnostics.CodeAnalysis;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using MongoDB.Driver;

namespace MapaTributario.API.Infrastructure.Repository;

[ExcludeFromCodeCoverage]
public class ConfiguracaoCrawlerRepository : IConfiguracaoCrawlerRepository
{
    private readonly IMongoCollection<ConfiguracaoCrawler> _configuracoes;

    public ConfiguracaoCrawlerRepository(IMongoDatabase database)
    {
        _configuracoes = database.GetCollection<ConfiguracaoCrawler>("configuracoesCrawler");
    }

    public async Task<ConfiguracaoCrawler?> ObterAtualAsync()
    {
        return await _configuracoes
            .Find(_ => true)
            .SortByDescending(c => c.CriadoEm)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> ExisteAlgumaAsync()
    {
        return await _configuracoes.Find(_ => true).AnyAsync();
    }

    public async Task<ConfiguracaoCrawler> CriarAsync(ConfiguracaoCrawler configuracao)
    {
        await _configuracoes.InsertOneAsync(configuracao);
        return configuracao;
    }

    public async Task AtualizarAsync(ConfiguracaoCrawler configuracao)
    {
        await _configuracoes.ReplaceOneAsync(
            c => c.Id == configuracao.Id,
            configuracao);
    }
}
