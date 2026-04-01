using MapaTributario.API.Domain.Entities;

namespace MapaTributario.API.Domain.Interfaces;

public interface IFilaProcessamentoRepository
{
    Task InsertManyAsync(IEnumerable<FilaProcessamento> itens);
    Task<IReadOnlyList<FilaProcessamento>> GetPendingAsync(int batchSize);
    Task UpdateStatusAsync(FilaProcessamento item);
    Task<Dictionary<StatusFila, long>> CountByStatusAsync();
    Task<FilaProcessamento?> GetByMunicipioAndServicoAsync(
        string codigoMunicipio,
        string codigoServico,
        string competencia);
    Task RevertProcessingTopendingAsync();
}
