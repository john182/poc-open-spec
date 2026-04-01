namespace MapaTributario.API.Application.Crawler.Contracts;

public class ExecutarCrawlerResponse
{
    public string? ExecucaoId { get; set; }
    public string Mensagem { get; set; } = null!;
}
