using FluentResults;
using MapaTributario.API.Application.Crawler.Contracts;

namespace MapaTributario.API.Application.Crawler;

public interface IConfiguracaoCrawlerAppService
{
    Task<Result<ConfiguracaoCrawlerResponse>> ObterConfiguracaoAtualAsync();
    Task<Result<ConfiguracaoCrawlerResponse>> AtualizarConfiguracaoAsync(AtualizarConfiguracaoCrawlerRequest request);
    Task<Result<ConfiguracaoCrawlerResponse>> AtualizarParcialmenteAsync(AtualizarParcialConfiguracaoCrawlerRequest request);
}
