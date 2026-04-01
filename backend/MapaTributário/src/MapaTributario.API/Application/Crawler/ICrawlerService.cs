using MapaTributario.API.Domain.Entities;

namespace MapaTributario.API.Application.Crawler;

public interface ICrawlerService
{
    Task<ExecucaoCrawler> ExecutarAsync(
        TipoExecucao tipo,
        bool forcarReprocessamento = false,
        IReadOnlyList<string>? filtroUfs = null,
        CancellationToken cancellationToken = default);

    bool EmExecucao { get; }
}
