using System.Diagnostics.CodeAnalysis;

namespace MapaTributario.API.Application.Crawler.Contracts;

[ExcludeFromCodeCoverage]
public class StatusCrawlerResponse
{
    public string Id { get; set; } = null!;
    public DateTime Inicio { get; set; }
    public DateTime? Fim { get; set; }
    public string Status { get; set; } = null!;
    public string Tipo { get; set; } = null!;
    public int TotalMunicipios { get; set; }
    public int TotalServicos { get; set; }
    public int Processados { get; set; }
    public int Erros { get; set; }
    public List<string> DetalhesErro { get; set; } = new();
    public bool TemCertificado { get; set; }
    public string? UfAtual { get; set; }
    public List<string> UfsProcessadas { get; set; } = new();
    public Dictionary<string, ProgressoUfResponse> ProgressoUfs { get; set; } = new();
}

[ExcludeFromCodeCoverage]
public class ProgressoUfResponse
{
    public string Uf { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int MunicipiosEncontrados { get; set; }
    public DateTime? Inicio { get; set; }
    public DateTime? Fim { get; set; }
}
