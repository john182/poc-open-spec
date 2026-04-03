using System.Diagnostics.CodeAnalysis;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using MongoDB.Driver;

namespace MapaTributario.API.Infrastructure.Repository;

[ExcludeFromCodeCoverage]
public class FilaProcessamentoRepository : IFilaProcessamentoRepository
{
    private readonly IMongoCollection<FilaProcessamento> _fila;

    public FilaProcessamentoRepository(IMongoDatabase database)
    {
        _fila = database.GetCollection<FilaProcessamento>("filaProcessamento");

        // Índice composto para consulta eficiente por UF + Status
        IndexKeysDefinition<FilaProcessamento> indexKeys = Builders<FilaProcessamento>.IndexKeys
            .Ascending(f => f.Uf)
            .Ascending(f => f.Status);

        _fila.Indexes.CreateOne(new CreateIndexModel<FilaProcessamento>(
            indexKeys,
            new CreateIndexOptions { Name = "ix_uf_status", Background = true }));
    }

    public async Task InsertManyAsync(IEnumerable<FilaProcessamento> itens)
    {
        List<FilaProcessamento> lista = itens.ToList();
        if (lista.Count == 0)
        {
            return;
        }

        try
        {
            await _fila.InsertManyAsync(lista, new InsertManyOptions { IsOrdered = false });
        }
        catch (MongoBulkWriteException)
        {
            // Ignore duplicate key errors — items already in queue are fine to skip.
            // With IsOrdered = false, all non-duplicate inserts succeed even if some fail.
        }
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

    public async Task<IReadOnlyList<FilaProcessamento>> GetPendingByUfAsync(string uf, int batchSize)
    {
        DateTime agora = DateTime.UtcNow;

        FilterDefinition<FilaProcessamento> filter = Builders<FilaProcessamento>.Filter.And(
            Builders<FilaProcessamento>.Filter.Eq(f => f.Uf, uf.ToUpperInvariant()),
            Builders<FilaProcessamento>.Filter.Or(
                Builders<FilaProcessamento>.Filter.Eq(f => f.Status, StatusFila.Pendente),
                Builders<FilaProcessamento>.Filter.And(
                    Builders<FilaProcessamento>.Filter.Eq(f => f.Status, StatusFila.Erro),
                    Builders<FilaProcessamento>.Filter.Lte(f => f.ProximaTentativa, agora)
                )
            )
        );

        return await _fila
            .Find(filter)
            .Limit(batchSize)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<string>> GetDistinctPendingUfsAsync()
    {
        DateTime agora = DateTime.UtcNow;

        FilterDefinition<FilaProcessamento> filter = Builders<FilaProcessamento>.Filter.Or(
            Builders<FilaProcessamento>.Filter.Eq(f => f.Status, StatusFila.Pendente),
            Builders<FilaProcessamento>.Filter.And(
                Builders<FilaProcessamento>.Filter.Eq(f => f.Status, StatusFila.Erro),
                Builders<FilaProcessamento>.Filter.Lte(f => f.ProximaTentativa, agora)
            )
        );

        List<string> ufs = await _fila
            .Distinct(f => f.Uf, filter)
            .ToListAsync();

        return ufs.AsReadOnly();
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
