namespace MapaTributario.API.Application.Crawler.Contracts;

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
}
