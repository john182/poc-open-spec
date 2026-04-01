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
    private readonly ICrawlerService _crawlerService;
    private readonly IExecucaoCrawlerRepository _execucaoRepository;

    public CrawlerController(
        ICrawlerService crawlerService,
        IExecucaoCrawlerRepository execucaoRepository)
    {
        _crawlerService = crawlerService;
        _execucaoRepository = execucaoRepository;
    }

    [HttpPost("executar")]
    public async Task<IActionResult> Executar([FromBody] ExecutarCrawlerRequest? request)
    {
        if (_crawlerService.EmExecucao)
        {
            return Conflict(new { erro = "Uma execucao ja esta em andamento" });
        }

        bool forcar = request?.ForcarReprocessamento ?? false;

        // Start execution in background
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        await _execucaoRepository.CreateAsync(execucao);

        _ = Task.Run(async () =>
        {
            try
            {
                await _crawlerService.ExecutarAsync(TipoExecucao.Manual, forcar);
            }
            catch (Exception)
            {
                // Logged inside CrawlerService
            }
        });

        return Accepted(new ExecutarCrawlerResponse
        {
            ExecucaoId = execucao.Id,
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
