using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using MongoDB.Driver;

namespace MapaTributario.API.Infrastructure.Repository;

public class ServicoRepository : IServicoRepository
{
    private readonly IMongoCollection<Servico> _servicos;

    public ServicoRepository(IMongoDatabase database)
    {
        _servicos = database.GetCollection<Servico>("servicos");
    }

    public async Task<IReadOnlyList<Servico>> GetAllAsync()
    {
        return await _servicos
            .Find(FilterDefinition<Servico>.Empty)
            .SortBy(s => s.CodigoTribNac)
            .ToListAsync();
    }

    public async Task<Servico?> GetByCodigoAsync(string codigoTribNac)
    {
        return await _servicos
            .Find(s => s.CodigoTribNac == codigoTribNac)
            .FirstOrDefaultAsync();
    }

    public async Task<long> CountAsync()
    {
        return await _servicos.CountDocumentsAsync(FilterDefinition<Servico>.Empty);
    }

    public async Task InsertManyAsync(IEnumerable<Servico> servicos)
    {
        var list = servicos.ToList();
        if (list.Count > 0)
        {
            await _servicos.InsertManyAsync(list);
        }
    }
}
