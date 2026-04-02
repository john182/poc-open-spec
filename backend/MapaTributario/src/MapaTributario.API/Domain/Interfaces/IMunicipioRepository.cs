using MapaTributario.API.Domain.Entities;

namespace MapaTributario.API.Domain.Interfaces;

public interface IMunicipioRepository
{
    Task<IReadOnlyList<Municipio>> GetByUfAsync(string siglaEstado);
    Task<Municipio?> GetByCodigoIbgeAsync(string codigoIbge);
    Task<long> CountAsync();
    Task InsertManyAsync(IEnumerable<Municipio> municipios);
}
