using MapaTributario.API.Domain.Entities;

namespace MapaTributario.API.Domain.Interfaces;

public interface IEstadoRepository
{
    Task<IReadOnlyList<Estado>> GetAllAsync();
    Task<Estado?> GetBySiglaAsync(string sigla);
    Task<long> CountAsync();
    Task InsertManyAsync(IEnumerable<Estado> estados);
}
