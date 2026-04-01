using System.Text.Json.Serialization;

namespace MapaTributario.API.Infrastructure.External.Contracts;

/// <summary>
/// Resposta real da API NFS-e: GET /parametrizacao/{codigoMunicipio}/convenio
/// Exemplo:
/// {
///   "parametrosConvenio": {
///     "aderenteAmbienteNacional": 1,
///     "aderenteEmissorNacional": 0,
///     "situacaoEmissaoPadraoContribuintesRFB": 1,
///     "aderenteMAN": 0,
///     "permiteAproveitametoDeCreditos": true
///   },
///   "mensagem": "Parâmetros do convênio recuperados com sucesso."
/// }
/// </summary>
public class ConvenioNfseResponse
{
    [JsonPropertyName("parametrosConvenio")]
    public ParametrosConvenio? ParametrosConvenio { get; set; }

    [JsonPropertyName("mensagem")]
    public string? Mensagem { get; set; }

    /// <summary>
    /// Município é considerado ativo se aderiu ao Ambiente Nacional (aderenteAmbienteNacional == 1).
    /// </summary>
    public bool Ativo => ParametrosConvenio?.AderenteAmbienteNacional == 1;
}

public class ParametrosConvenio
{
    [JsonPropertyName("aderenteAmbienteNacional")]
    public int AderenteAmbienteNacional { get; set; }

    [JsonPropertyName("aderenteEmissorNacional")]
    public int AderenteEmissorNacional { get; set; }

    [JsonPropertyName("situacaoEmissaoPadraoContribuintesRFB")]
    public int SituacaoEmissaoPadraoContribuintesRFB { get; set; }

    [JsonPropertyName("aderenteMAN")]
    public int AderenteMAN { get; set; }

    [JsonPropertyName("permiteAproveitametoDeCreditos")]
    public bool PermiteAproveitametoDeCreditos { get; set; }
}
