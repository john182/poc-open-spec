using FluentResults;

namespace MapaTributario.API.Application.Errors;

public class CrawlerDesativadoError : Error
{
    public CrawlerDesativadoError()
        : base("Crawler desativado pela configuração atual") { }
}
