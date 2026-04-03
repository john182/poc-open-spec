using FluentValidation;
using MapaTributario.API.Application.Crawler;
using MapaTributario.API.Application.Crawler.Contracts;
using MapaTributario.API.Application.Errors;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapaTributario.API.Controllers;

[ApiController]
[Route("api/v1/crawler")]
[Authorize(Roles = "Admin")]
public class CrawlerController : ControllerBase
{
    private readonly ICrawlerExecutionGuard _executionGuard;
    private readonly IExecucaoCrawlerRepository _execucaoRepository;
    private readonly IServiceProvider _serviceProvider;
    private readonly ICertificadoStore _certificadoStore;
    private readonly IConfiguracaoCrawlerAppService _configuracaoAppService;

    public CrawlerController(
        ICrawlerExecutionGuard executionGuard,
        IExecucaoCrawlerRepository execucaoRepository,
        IServiceProvider serviceProvider,
        ICertificadoStore certificadoStore,
        IConfiguracaoCrawlerAppService configuracaoAppService)
    {
        _executionGuard = executionGuard;
        _execucaoRepository = execucaoRepository;
        _serviceProvider = serviceProvider;
        _certificadoStore = certificadoStore;
        _configuracaoAppService = configuracaoAppService;
    }

    [HttpPost("executar")]
    public IActionResult Executar([FromBody] ExecutarCrawlerRequest? request)
    {
        if (!_certificadoStore.HasCertificate())
        {
            return BadRequest(new { erro = "Nenhum certificado digital disponível. Faça upload via POST /api/v1/crawler/certificado" });
        }

        if (_executionGuard.IsRunning)
        {
            return Conflict(new { erro = "Uma execucao ja esta em andamento" });
        }

        bool forcar = request?.ForcarReprocessamento ?? false;
        bool capitaisPrimeiro = request?.CapitaisPrimeiro ?? false;
        List<string>? ufs = request?.Ufs;

        // Fire-and-forget with a new scope (CrawlerService is Scoped)
        _ = Task.Run(async () =>
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            ICrawlerService crawlerService = scope.ServiceProvider.GetRequiredService<ICrawlerService>();
            try
            {
                if (capitaisPrimeiro)
                {
                    // Fase 1: somente capitais estaduais (EhCapital = true)
                    var resultadoCapitais = await crawlerService.ExecutarAsync(
                        TipoExecucao.Manual, forcar, filtroUfs: null, filtroCapital: true);

                    if (resultadoCapitais.IsFailed)
                    {
                        // Logged inside CrawlerService — aborta a segunda fase
                        return;
                    }

                    // Fase 2: somente municípios não-capitais (EhCapital = false)
                    await crawlerService.ExecutarAsync(
                        TipoExecucao.Manual, forcar, filtroUfs: null, filtroCapital: false);
                }
                else
                {
                    // Execução normal: processa tudo junto, com filtro de UFs opcional
                    await crawlerService.ExecutarAsync(TipoExecucao.Manual, forcar, ufs);
                }
            }
            catch (Exception)
            {
                // Logged inside CrawlerService
            }
        });

        string mensagem = capitaisPrimeiro
            ? "Execução iniciada: capitais primeiro, depois demais municípios"
            : "Execucao iniciada com sucesso";

        return Accepted(new ExecutarCrawlerResponse
        {
            Mensagem = mensagem
        });
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        ExecucaoCrawler? latest = await _execucaoRepository.GetLatestAsync();

        if (latest is null)
        {
            return Ok(new StatusCrawlerResponse
            {
                Id = string.Empty,
                Inicio = DateTime.MinValue,
                Fim = null,
                Status = "NenhumaExecucao",
                Tipo = string.Empty,
                TotalMunicipios = 0,
                TotalServicos = 0,
                Processados = 0,
                Erros = 0,
                DetalhesErro = new(),
                TemCertificado = _certificadoStore.HasCertificate()
            });
        }

        return Ok(MapToResponse(latest));
    }

    [HttpGet("execucoes")]
    public async Task<IActionResult> Execucoes()
    {
        IReadOnlyList<ExecucaoCrawler> recentes = await _execucaoRepository.GetRecentAsync(20);

        return Ok(recentes.Select(MapToResponse).ToList());
    }

    [HttpGet("configuracao")]
    public async Task<IActionResult> ObterConfiguracao()
    {
        var resultado = await _configuracaoAppService.ObterConfiguracaoAtualAsync();

        if (resultado.IsFailed)
        {
            var erroNaoEncontrado = resultado.Errors.OfType<NotFoundError>().FirstOrDefault();
            if (erroNaoEncontrado is not null)
            {
                return NotFound(new { erro = erroNaoEncontrado.Message });
            }

            return BadRequest(new { erro = resultado.Errors.First().Message });
        }

        return Ok(resultado.Value);
    }

    [HttpPut("configuracao")]
    public async Task<IActionResult> AtualizarConfiguracao(
        [FromBody] AtualizarConfiguracaoCrawlerRequest request,
        [FromServices] IValidator<AtualizarConfiguracaoCrawlerRequest> validator)
    {
        var validacao = await validator.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return BadRequest(new { erro = "Validação falhou", detalhes = validacao.Errors.Select(e => e.ErrorMessage).ToArray() });
        }

        var resultado = await _configuracaoAppService.AtualizarConfiguracaoAsync(request);

        if (resultado.IsFailed)
        {
            var erroNaoEncontrado = resultado.Errors.OfType<NotFoundError>().FirstOrDefault();
            if (erroNaoEncontrado is not null)
            {
                return NotFound(new { erro = erroNaoEncontrado.Message });
            }

            return BadRequest(new { erro = resultado.Errors.First().Message });
        }

        return Ok(resultado.Value);
    }

    [HttpPatch("configuracao")]
    public async Task<IActionResult> AtualizarParcialConfiguracao(
        [FromBody] AtualizarParcialConfiguracaoCrawlerRequest request,
        [FromServices] IValidator<AtualizarParcialConfiguracaoCrawlerRequest> validator)
    {
        var validacao = await validator.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return BadRequest(new { erro = "Validação falhou", detalhes = validacao.Errors.Select(e => e.ErrorMessage).ToArray() });
        }

        var resultado = await _configuracaoAppService.AtualizarParcialmenteAsync(request);

        if (resultado.IsFailed)
        {
            var erroNaoEncontrado = resultado.Errors.OfType<NotFoundError>().FirstOrDefault();
            if (erroNaoEncontrado is not null)
            {
                return NotFound(new { erro = erroNaoEncontrado.Message });
            }

            return BadRequest(new { erro = resultado.Errors.First().Message });
        }

        return Ok(resultado.Value);
    }

    private StatusCrawlerResponse MapToResponse(ExecucaoCrawler execucao)
    {
        return new StatusCrawlerResponse
        {
            Id = execucao.Id,
            Inicio = execucao.Inicio,
            Fim = execucao.Fim,
            Status = execucao.Status.ToString(),
            Tipo = execucao.Tipo.ToString(),
            FaseAtual = execucao.FaseAtual.ToString(),
            TotalMunicipios = execucao.TotalMunicipios,
            TotalServicos = execucao.TotalServicos,
            Processados = execucao.Processados,
            Erros = execucao.Erros,
            DetalhesErro = execucao.DetalhesErro,
            TemCertificado = _certificadoStore.HasCertificate(),
            UfsEmAndamento = execucao.UfsEmAndamento.ToList(),
            UfsProcessadas = execucao.UfsProcessadas,
            ProgressoUfs = execucao.ProgressoUfs.ToDictionary(
                kvp => kvp.Key,
                kvp => new ProgressoUfResponse
                {
                    Uf = kvp.Value.Uf,
                    Status = kvp.Value.Status.ToString(),
                    MunicipiosEncontrados = kvp.Value.MunicipiosEncontrados,
                    MunicipiosAtivos = kvp.Value.MunicipiosAtivos,
                    Inicio = kvp.Value.Inicio,
                    Fim = kvp.Value.Fim
                })
        };
    }
}
