using MapaTributario.API.Domain.Entities;

namespace MapaTributario.API.Domain.Interfaces;

public interface IExecucaoCrawlerRepository
{
    Task<ExecucaoCrawler> CreateAsync(ExecucaoCrawler execucao);
    Task UpdateAsync(ExecucaoCrawler execucao);
    Task<ExecucaoCrawler?> GetLatestAsync();
    Task<IReadOnlyList<ExecucaoCrawler>> GetRecentAsync(int limit = 20);
}
