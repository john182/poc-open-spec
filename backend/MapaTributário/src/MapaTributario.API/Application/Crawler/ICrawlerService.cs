using MapaTributario.API.Domain.Entities;

namespace MapaTributario.API.Application.Crawler;

public interface ICrawlerService
{
    Task<ExecucaoCrawler> ExecutarAsync(
        TipoExecucao tipo,
        bool forcarReprocessamento = false,
        CancellationToken cancellationToken = default);

    bool EmExecucao { get; }
}
