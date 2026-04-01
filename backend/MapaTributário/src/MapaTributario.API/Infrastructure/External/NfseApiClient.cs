using System.Net;
using System.Net.Http.Json;
using MapaTributario.API.Infrastructure.External.Contracts;

namespace MapaTributario.API.Infrastructure.External;

public class NfseApiClientOptions
{
    public const string SectionName = "NfseApi";

    public string BaseUrl { get; set; } = "https://adn.nfse.gov.br";
    public string CertificatePath { get; set; } = "/certs/client.pfx";
    public string CertificatePassword { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}

public class NfseApiClient : INfseApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NfseApiClient> _logger;

    public NfseApiClient(HttpClient httpClient, ILogger<NfseApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AliquotaNfseResponse?> GetAliquotaAsync(
        string codigoMunicipio,
        string codigoServico,
        string competencia,
        CancellationToken cancellationToken = default)
    {
        string url = $"/parametrizacao/{codigoMunicipio}/{codigoServico}/{competencia}/aliquota";

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<AliquotaNfseResponse>(cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<ConvenioNfseResponse?> GetConvenioAsync(
        string codigoMunicipio,
        CancellationToken cancellationToken = default)
    {
        string url = $"/parametrizacao/{codigoMunicipio}/convenio";

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<ConvenioNfseResponse>(cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

}
