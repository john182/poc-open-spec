using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using MongoDB.Driver;

namespace MapaTributario.API.Infrastructure.Repository;

public class MunicipioRepository : IMunicipioRepository
{
    private readonly IMongoCollection<Municipio> _municipios;

    public MunicipioRepository(IMongoDatabase database)
    {
        _municipios = database.GetCollection<Municipio>("municipios");
    }

    public async Task<IReadOnlyList<Municipio>> GetByUfAsync(string siglaEstado)
    {
        return await _municipios
            .Find(m => m.SiglaEstado == siglaEstado.ToUpperInvariant())
            .SortBy(m => m.Nome)
            .ToListAsync();
    }

    public async Task<Municipio?> GetByCodigoIbgeAsync(string codigoIbge)
    {
        return await _municipios
            .Find(m => m.CodigoIbge == codigoIbge)
            .FirstOrDefaultAsync();
    }

    public async Task<long> CountAsync()
    {
        return await _municipios.CountDocumentsAsync(FilterDefinition<Municipio>.Empty);
    }

    public async Task InsertManyAsync(IEnumerable<Municipio> municipios)
    {
        var list = municipios.ToList();
        if (list.Count > 0)
        {
            await _municipios.InsertManyAsync(list);
        }
    }
}
