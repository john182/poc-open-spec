namespace MapaTributario.API.Application.Crawler.Contracts;

public class ExecutarCrawlerRequest
{
    public bool ForcarReprocessamento { get; set; }

    /// <summary>
    /// Lista de UFs para filtrar a execução (ex: ["SE", "BA"]).
    /// Se vazio ou nulo, processa todos os estados.
    /// </summary>
    public List<string>? Ufs { get; set; }
}
