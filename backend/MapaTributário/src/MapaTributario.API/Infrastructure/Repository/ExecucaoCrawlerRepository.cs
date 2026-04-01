using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using MongoDB.Driver;

namespace MapaTributario.API.Infrastructure.Repository;

public class ExecucaoCrawlerRepository : IExecucaoCrawlerRepository
{
    private readonly IMongoCollection<ExecucaoCrawler> _execucoes;

    public ExecucaoCrawlerRepository(IMongoDatabase database)
    {
        _execucoes = database.GetCollection<ExecucaoCrawler>("execucoesCrawler");
    }

    public async Task<ExecucaoCrawler> CreateAsync(ExecucaoCrawler execucao)
    {
        await _execucoes.InsertOneAsync(execucao);
        return execucao;
    }

    public async Task UpdateAsync(ExecucaoCrawler execucao)
    {
        await _execucoes.ReplaceOneAsync(
            e => e.Id == execucao.Id,
            execucao);
    }

    public async Task<ExecucaoCrawler?> GetLatestAsync()
    {
        return await _execucoes
            .Find(_ => true)
            .SortByDescending(e => e.Inicio)
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<ExecucaoCrawler>> GetRecentAsync(int limit = 20)
    {
        return await _execucoes
            .Find(_ => true)
            .SortByDescending(e => e.Inicio)
            .Limit(limit)
            .ToListAsync();
    }
}
