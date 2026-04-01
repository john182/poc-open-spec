using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using MongoDB.Driver;

namespace MapaTributario.API.Infrastructure.Repository;

public class EstadoRepository : IEstadoRepository
{
    private readonly IMongoCollection<Estado> _estados;

    public EstadoRepository(IMongoDatabase database)
    {
        _estados = database.GetCollection<Estado>("estados");

        var indexKeys = Builders<Estado>.IndexKeys.Ascending(e => e.Sigla);
        var indexOptions = new CreateIndexOptions { Unique = true };
        _estados.Indexes.CreateOne(new CreateIndexModel<Estado>(indexKeys, indexOptions));
    }

    public async Task<IReadOnlyList<Estado>> GetAllAsync()
    {
        return await _estados
            .Find(FilterDefinition<Estado>.Empty)
            .SortBy(e => e.Nome)
            .ToListAsync();
    }

    public async Task<Estado?> GetBySiglaAsync(string sigla)
    {
        return await _estados
            .Find(e => e.Sigla == sigla.ToUpperInvariant())
            .FirstOrDefaultAsync();
    }

    public async Task<long> CountAsync()
    {
        return await _estados.CountDocumentsAsync(FilterDefinition<Estado>.Empty);
    }

    public async Task InsertManyAsync(IEnumerable<Estado> estados)
    {
        var list = estados.ToList();
        if (list.Count > 0)
        {
            await _estados.InsertManyAsync(list);
        }
    }
}
