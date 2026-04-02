namespace MapaTributario.API.Application.Crawler.Contracts;

public class ExecutarCrawlerRequest
{
    public bool ForcarReprocessamento { get; set; }

    /// <summary>
    /// Lista de UFs para filtrar a execução (ex: ["SE", "BA"]).
    /// Se vazio ou nulo, processa todos os estados.
    /// </summary>
    public List<string>? Ufs { get; set; }

    /// <summary>
    /// Quando true, executa em duas fases sequenciais:
    /// 1ª fase — processa somente os municípios que são capitais estaduais (EhCapital = true)
    /// 2ª fase — após todas as capitais concluírem, processa os demais municípios (EhCapital = false)
    /// Ignora o filtro de UFs quando ativo (processa todos os estados).
    /// </summary>
    public bool CapitaisPrimeiro { get; set; }
}
