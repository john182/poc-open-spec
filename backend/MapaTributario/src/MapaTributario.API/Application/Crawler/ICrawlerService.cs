using FluentResults;
using MapaTributario.API.Domain.Entities;

namespace MapaTributario.API.Application.Crawler;

public interface ICrawlerService
{
    /// <summary>
    /// Executa o crawler para coleta de alíquotas.
    /// </summary>
    /// <param name="tipo">Tipo da execução (Manual ou Agendado).</param>
    /// <param name="forcarReprocessamento">Quando true, reprocessa mesmo serviços já coletados.</param>
    /// <param name="filtroUfs">Filtra por UFs específicas. Nulo = todas.</param>
    /// <param name="filtroCapital">
    /// null  = sem filtro (processa capitais e não-capitais juntos).
    /// true  = somente municípios que são capitais estaduais (EhCapital = true).
    /// false = somente municípios que NÃO são capitais (EhCapital = false).
    /// </param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task<Result<ExecucaoCrawler>> ExecutarAsync(
        TipoExecucao tipo,
        bool forcarReprocessamento = false,
        IReadOnlyList<string>? filtroUfs = null,
        bool? filtroCapital = null,
        CancellationToken cancellationToken = default);

    bool EmExecucao { get; }
}
