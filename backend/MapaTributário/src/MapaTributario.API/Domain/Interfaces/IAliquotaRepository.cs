using MapaTributario.API.Domain.Entities;

namespace MapaTributario.API.Domain.Interfaces;

public interface IAliquotaRepository
{
    Task<(IReadOnlyList<Aliquota> Items, long Total)> GetByMunicipioAsync(
        string codigoIbge,
        int pagina,
        int tamanhoPagina,
        string? codigoServico = null,
        string? descricao = null,
        decimal? aliquotaMin = null,
        decimal? aliquotaMax = null,
        string? competencia = null);

    Task<Aliquota?> GetDetalheAsync(string codigoIbge, string codigoServico);

    Task UpsertAsync(Aliquota aliquota);

    Task<bool> ExistsAsync(string codigoMunicipio, string codigoServico, string competencia);
}
