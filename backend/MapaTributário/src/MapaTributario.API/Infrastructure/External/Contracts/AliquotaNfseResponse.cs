using System.Text.Json.Serialization;

namespace MapaTributario.API.Infrastructure.External.Contracts;

/// <summary>
/// Resposta real da API NFS-e: GET /parametrizacao/{codigoMunicipio}/{codigoServico}/{competencia}/aliquota
/// Exemplo:
/// {
///   "aliquotas": {
///     "01.01.01.000": [
///       { "Incidencia": "SIM", "Aliq": 5.00, "DtIni": "2023-10-20T00:00:00", "DtFim": null }
///     ]
///   },
///   "mensagem": "Alíquotas recuperadas com sucesso."
/// }
/// </summary>
public class AliquotaNfseResponse
{
    [JsonPropertyName("aliquotas")]
    public Dictionary<string, List<AliquotaItem>>? Aliquotas { get; set; }

    [JsonPropertyName("mensagem")]
    public string? Mensagem { get; set; }

    /// <summary>
    /// Verifica se a resposta contém alíquotas válidas.
    /// </summary>
    public bool TemDados => Aliquotas is { Count: > 0 };
}

public class AliquotaItem
{
    [JsonPropertyName("Incidencia")]
    public string? Incidencia { get; set; }

    [JsonPropertyName("Aliq")]
    public decimal Aliq { get; set; }

    [JsonPropertyName("DtIni")]
    public DateTime? DtIni { get; set; }

    [JsonPropertyName("DtFim")]
    public DateTime? DtFim { get; set; }

    /// <summary>
    /// Retorna true se a alíquota está vigente (DtFim é null ou no futuro).
    /// </summary>
    public bool Vigente => DtFim is null || DtFim > DateTime.UtcNow;
}
