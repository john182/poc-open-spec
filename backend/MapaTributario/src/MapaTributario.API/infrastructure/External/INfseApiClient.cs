using MapaTributario.API.Infrastructure.External.Contracts;

namespace MapaTributario.API.Infrastructure.External;

public interface INfseApiClient
{
    Task<AliquotaNfseResponse?> GetAliquotaAsync(
        string codigoMunicipio,
        string codigoServico,
        string competencia,
        CancellationToken cancellationToken = default);

    Task<ConvenioNfseResponse?> GetConvenioAsync(
        string codigoMunicipio,
        CancellationToken cancellationToken = default);
}
