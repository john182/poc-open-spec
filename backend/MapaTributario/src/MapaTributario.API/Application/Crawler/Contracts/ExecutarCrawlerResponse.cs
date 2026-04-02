using System.Diagnostics.CodeAnalysis;

namespace MapaTributario.API.Application.Crawler.Contracts;

[ExcludeFromCodeCoverage]
public class ExecutarCrawlerResponse
{
    public string? ExecucaoId { get; set; }
    public string Mensagem { get; set; } = null!;
}
