using MapaTributario.API.Domain.Entities;

namespace MapaTributario.API.Domain.Interfaces;

public interface IServicoRepository
{
    Task<IReadOnlyList<Servico>> GetAllAsync();
    Task<Servico?> GetByCodigoAsync(string codigoTribNac);
    Task<long> CountAsync();
    Task InsertManyAsync(IEnumerable<Servico> servicos);
}
