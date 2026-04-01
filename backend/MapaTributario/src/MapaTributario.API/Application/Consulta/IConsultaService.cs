using FluentResults;
using MapaTributario.API.Application.Consulta.Contracts;

namespace MapaTributario.API.Application.Consulta;

public interface IConsultaService
{
    Task<Result<IReadOnlyList<EstadoResponse>>> ListarEstadosAsync();

    Task<Result<MunicipiosUfResponse>> ListarMunicipiosPorUfAsync(string uf);

    Task<Result<PaginatedResponse<AliquotaResponse>>> ListarAliquotasPorMunicipioAsync(
        string codigoIbge,
        AliquotaQueryParams queryParams);

    Task<Result<AliquotaDetalheResponse>> ObterDetalheAliquotaAsync(
        string codigoIbge,
        string codigoServico);
}
