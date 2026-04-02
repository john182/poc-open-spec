using FluentResults;
using MapaTributario.API.Application.Crawler.Contracts;
using MapaTributario.API.Application.Errors;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;

namespace MapaTributario.API.Application.Crawler;

public class ConfiguracaoCrawlerAppService : IConfiguracaoCrawlerAppService
{
    private readonly IConfiguracaoCrawlerRepository _repository;
    private readonly ILogger<ConfiguracaoCrawlerAppService> _logger;

    public ConfiguracaoCrawlerAppService(
        IConfiguracaoCrawlerRepository repository,
        ILogger<ConfiguracaoCrawlerAppService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<ConfiguracaoCrawlerResponse>> ObterConfiguracaoAtualAsync()
    {
        ConfiguracaoCrawler? configuracao = await _repository.ObterAtivaAsync();

        if (configuracao is null)
        {
            return Result.Fail<ConfiguracaoCrawlerResponse>(
                new NotFoundError("Nenhuma configuração ativa encontrada"));
        }

        return Result.Ok(MapearParaResponse(configuracao));
    }

    public async Task<Result<ConfiguracaoCrawlerResponse>> AtualizarConfiguracaoAsync(
        AtualizarConfiguracaoCrawlerRequest request)
    {
        ConfiguracaoCrawler? configuracao = await _repository.ObterAtivaAsync();

        if (configuracao is null)
        {
            return Result.Fail<ConfiguracaoCrawlerResponse>(
                new NotFoundError("Nenhuma configuração ativa encontrada"));
        }

        configuracao.Atualizar(
            request.CronSchedule,
            request.LimiteRequisicoesPorSegundo,
            request.OrcamentoDiario,
            request.TamanheLoteCertificado,
            request.PausaLoteSegundos,
            request.TamanheLoteMongo,
            request.MaxTentativas,
            request.LimiteParadaAntecipada,
            request.MaxDesdobramento,
            request.MaxDetalhamento,
            request.MaxFalhasConsecutivasDetalhamento,
            request.MaxFalhasConsecutivasDesdobramento,
            request.MaxItensParalelos,
            request.CodigosSondagem,
            request.ValidadeDiasProcessamento,
            request.CircuitBreakerLimiarErroPercent,
            request.CircuitBreakerJanelaAvaliacaoSegundos,
            request.CircuitBreakerPausaSegundos,
            request.CircuitBreakerAmostraMinima,
            request.Ativo);

        await _repository.AtualizarAsync(configuracao);

        _logger.LogInformation(
            "Configuração do crawler atualizada (Id={Id})",
            configuracao.Id);

        return Result.Ok(MapearParaResponse(configuracao));
    }

    public async Task<Result<ConfiguracaoCrawlerResponse>> AtualizarParcialmenteAsync(
        AtualizarParcialConfiguracaoCrawlerRequest request)
    {
        ConfiguracaoCrawler? configuracao = await _repository.ObterAtivaAsync();

        if (configuracao is null)
        {
            return Result.Fail<ConfiguracaoCrawlerResponse>(
                new NotFoundError("Nenhuma configuração ativa encontrada"));
        }

        configuracao.AtualizarParcial(
            request.CronSchedule,
            request.LimiteRequisicoesPorSegundo,
            request.OrcamentoDiario,
            request.TamanheLoteCertificado,
            request.PausaLoteSegundos,
            request.TamanheLoteMongo,
            request.MaxTentativas,
            request.LimiteParadaAntecipada,
            request.MaxDesdobramento,
            request.MaxDetalhamento,
            request.MaxFalhasConsecutivasDetalhamento,
            request.MaxFalhasConsecutivasDesdobramento,
            request.MaxItensParalelos,
            request.CodigosSondagem,
            request.ValidadeDiasProcessamento,
            request.CircuitBreakerLimiarErroPercent,
            request.CircuitBreakerJanelaAvaliacaoSegundos,
            request.CircuitBreakerPausaSegundos,
            request.CircuitBreakerAmostraMinima,
            request.Ativo);

        await _repository.AtualizarAsync(configuracao);

        _logger.LogInformation(
            "Configuração do crawler atualizada parcialmente (Id={Id})",
            configuracao.Id);

        return Result.Ok(MapearParaResponse(configuracao));
    }

    private static ConfiguracaoCrawlerResponse MapearParaResponse(ConfiguracaoCrawler entidade)
    {
        return new ConfiguracaoCrawlerResponse
        {
            Id = entidade.Id,
            CronSchedule = entidade.CronSchedule,
            LimiteRequisicoesPorSegundo = entidade.LimiteRequisicoesPorSegundo,
            OrcamentoDiario = entidade.OrcamentoDiario,
            TamanheLoteCertificado = entidade.TamanheLoteCertificado,
            PausaLoteSegundos = entidade.PausaLoteSegundos,
            TamanheLoteMongo = entidade.TamanheLoteMongo,
            MaxTentativas = entidade.MaxTentativas,
            LimiteParadaAntecipada = entidade.LimiteParadaAntecipada,
            MaxDesdobramento = entidade.MaxDesdobramento,
            MaxDetalhamento = entidade.MaxDetalhamento,
            MaxFalhasConsecutivasDetalhamento = entidade.MaxFalhasConsecutivasDetalhamento,
            MaxFalhasConsecutivasDesdobramento = entidade.MaxFalhasConsecutivasDesdobramento,
            MaxItensParalelos = entidade.MaxItensParalelos,
            CodigosSondagem = entidade.CodigosSondagem,
            ValidadeDiasProcessamento = entidade.ValidadeDiasProcessamento,
            CircuitBreakerLimiarErroPercent = entidade.CircuitBreakerLimiarErroPercent,
            CircuitBreakerJanelaAvaliacaoSegundos = entidade.CircuitBreakerJanelaAvaliacaoSegundos,
            CircuitBreakerPausaSegundos = entidade.CircuitBreakerPausaSegundos,
            CircuitBreakerAmostraMinima = entidade.CircuitBreakerAmostraMinima,
            Ativo = entidade.Ativo,
            CriadoEm = entidade.CriadoEm,
            AtualizadoEm = entidade.AtualizadoEm
        };
    }
}
