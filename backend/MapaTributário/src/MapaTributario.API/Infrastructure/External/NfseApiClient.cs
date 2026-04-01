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
        // API requires format XX.XX.XX.XXX (with desdobramento)
        string codigoFormatado = FormatarCodigoServico(codigoServico);
        string url = $"/parametrizacao/{codigoMunicipio}/{codigoFormatado}/{competencia}/aliquota";

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                string body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "BadRequest from NFS-e API for {Url}: {Body}", url, body);
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

    /// <summary>
    /// Formata código de serviço para o formato esperado pela API: XX.XX.XX.XXX
    /// Aceita:
    ///   "01.01.01" → "01.01.01.000"
    ///   "01.01.00" → "01.01.00.000"
    ///   "01.01.01.000" → "01.01.01.000" (já formatado)
    ///   "010101000" → "01.01.01.000"
    /// </summary>
    internal static string FormatarCodigoServico(string codigo)
    {
        string clean = codigo.Replace(".", "");

        // If we have 9 digits (6 service + 3 desdobramento), format with dots
        if (clean.Length == 9)
        {
            return $"{clean[..2]}.{clean[2..4]}.{clean[4..6]}.{clean[6..9]}";
        }

        // If we have 6 digits (service only), append "000" desdobramento
        if (clean.Length == 6)
        {
            return $"{clean[..2]}.{clean[2..4]}.{clean[4..6]}.000";
        }

        // Fallback: return as-is (shouldn't happen with valid data)
        return codigo;
    }
}
