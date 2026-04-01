using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using MongoDB.Driver;

namespace MapaTributario.API.Infrastructure.Repository;

public class FilaProcessamentoRepository : IFilaProcessamentoRepository
{
    private readonly IMongoCollection<FilaProcessamento> _fila;

    public FilaProcessamentoRepository(IMongoDatabase database)
    {
        _fila = database.GetCollection<FilaProcessamento>("fila_processamento");

        var uniqueIndex = Builders<FilaProcessamento>.IndexKeys
            .Ascending(f => f.CodigoMunicipio)
            .Ascending(f => f.CodigoServico)
            .Ascending(f => f.Competencia);
        _fila.Indexes.CreateOne(new CreateIndexModel<FilaProcessamento>(
            uniqueIndex,
            new CreateIndexOptions { Unique = true }));

        var statusIndex = Builders<FilaProcessamento>.IndexKeys
            .Ascending(f => f.Status)
            .Ascending(f => f.ProximaTentativa);
        _fila.Indexes.CreateOne(new CreateIndexModel<FilaProcessamento>(statusIndex));

        var execucaoIndex = Builders<FilaProcessamento>.IndexKeys
            .Ascending(f => f.ExecucaoId);
        _fila.Indexes.CreateOne(new CreateIndexModel<FilaProcessamento>(execucaoIndex));
    }

    public async Task InsertManyAsync(IEnumerable<FilaProcessamento> itens)
    {
        List<FilaProcessamento> lista = itens.ToList();
        if (lista.Count == 0)
        {
            return;
        }

        await _fila.InsertManyAsync(lista);
    }

    public async Task<IReadOnlyList<FilaProcessamento>> GetPendingAsync(int batchSize)
    {
        DateTime agora = DateTime.UtcNow;

        FilterDefinition<FilaProcessamento> filter = Builders<FilaProcessamento>.Filter.Or(
            Builders<FilaProcessamento>.Filter.Eq(f => f.Status, StatusFila.Pendente),
            Builders<FilaProcessamento>.Filter.And(
                Builders<FilaProcessamento>.Filter.Eq(f => f.Status, StatusFila.Erro),
                Builders<FilaProcessamento>.Filter.Lte(f => f.ProximaTentativa, agora)
            )
        );

        return await _fila
            .Find(filter)
            .Limit(batchSize)
            .ToListAsync();
    }

    public async Task UpdateStatusAsync(FilaProcessamento item)
    {
        await _fila.ReplaceOneAsync(f => f.Id == item.Id, item);
    }

    public async Task<Dictionary<StatusFila, long>> CountByStatusAsync()
    {
        var pipeline = _fila.Aggregate()
            .Group(
                f => f.Status,
                g => new { Status = g.Key, Count = g.Count() });

        var results = await pipeline.ToListAsync();

        return results.ToDictionary(r => r.Status, r => (long)r.Count);
    }

    public async Task<FilaProcessamento?> GetByMunicipioAndServicoAsync(
        string codigoMunicipio,
        string codigoServico,
        string competencia)
    {
        return await _fila
            .Find(f => f.CodigoMunicipio == codigoMunicipio
                && f.CodigoServico == codigoServico
                && f.Competencia == competencia)
            .FirstOrDefaultAsync();
    }

    public async Task RevertProcessingTopendingAsync()
    {
        FilterDefinition<FilaProcessamento> filter = Builders<FilaProcessamento>.Filter
            .Eq(f => f.Status, StatusFila.Processando);

        UpdateDefinition<FilaProcessamento> update = Builders<FilaProcessamento>.Update
            .Set(f => f.Status, StatusFila.Pendente)
            .Set(f => f.AtualizadoEm, DateTime.UtcNow);

        await _fila.UpdateManyAsync(filter, update);
    }
}
