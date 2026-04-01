using MapaTributario.API.Application.Crawler;
using MapaTributario.API.Application.Crawler.Contracts;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapaTributario.API.Controllers;

[ApiController]
[Route("api/v1/crawler")]
[Authorize]
public class CrawlerController : ControllerBase
{
    private readonly ICrawlerExecutionGuard _executionGuard;
    private readonly IExecucaoCrawlerRepository _execucaoRepository;
    private readonly IServiceProvider _serviceProvider;

    public CrawlerController(
        ICrawlerExecutionGuard executionGuard,
        IExecucaoCrawlerRepository execucaoRepository,
        IServiceProvider serviceProvider)
    {
        _executionGuard = executionGuard;
        _execucaoRepository = execucaoRepository;
        _serviceProvider = serviceProvider;
    }

    [HttpPost("executar")]
    public IActionResult Executar([FromBody] ExecutarCrawlerRequest? request)
    {
        if (_executionGuard.IsRunning)
        {
            return Conflict(new { erro = "Uma execucao ja esta em andamento" });
        }

        bool forcar = request?.ForcarReprocessamento ?? false;

        // Fire-and-forget with a new scope (CrawlerService is Scoped)
        _ = Task.Run(async () =>
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            ICrawlerService crawlerService = scope.ServiceProvider.GetRequiredService<ICrawlerService>();
            try
            {
                await crawlerService.ExecutarAsync(Domain.Entities.TipoExecucao.Manual, forcar);
            }
            catch (Exception)
            {
                // Logged inside CrawlerService
            }
        });

        return Accepted(new ExecutarCrawlerResponse
        {
            Mensagem = "Execucao iniciada com sucesso"
        });
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        ExecucaoCrawler? latest = await _execucaoRepository.GetLatestAsync();

        if (latest is null)
        {
            return NotFound(new { erro = "Nenhuma execucao encontrada" });
        }

        return Ok(MapToResponse(latest));
    }

    [HttpGet("execucoes")]
    public async Task<IActionResult> Execucoes()
    {
        IReadOnlyList<ExecucaoCrawler> recentes = await _execucaoRepository.GetRecentAsync(20);

        return Ok(recentes.Select(MapToResponse).ToList());
    }

    private static StatusCrawlerResponse MapToResponse(ExecucaoCrawler execucao)
    {
        return new StatusCrawlerResponse
        {
            Id = execucao.Id,
            Inicio = execucao.Inicio,
            Fim = execucao.Fim,
            Status = execucao.Status.ToString(),
            Tipo = execucao.Tipo.ToString(),
            TotalMunicipios = execucao.TotalMunicipios,
            TotalServicos = execucao.TotalServicos,
            Processados = execucao.Processados,
            Erros = execucao.Erros,
            DetalhesErro = execucao.DetalhesErro
        };
    }
}
