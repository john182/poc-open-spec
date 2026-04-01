using MapaTributario.API.Domain.Entities;
using MongoDB.Driver;

namespace MapaTributario.API.Infrastructure.Repository.Mongo;

/// <summary>
/// Centralizes all MongoDB index creation. Called once at application startup.
/// </summary>
public sealed class MongoIndexSetup
{
    private readonly IMongoDatabase _database;

    public MongoIndexSetup(IMongoDatabase database)
    {
        _database = database;
    }

    public async Task ApplyAsync()
    {
        await CreateUserIndexesAsync();
        await CreateEstadoIndexesAsync();
        await CreateMunicipioIndexesAsync();
        await CreateServicoIndexesAsync();
        await CreateAliquotaIndexesAsync();
        await CreateExecucaoCrawlerIndexesAsync();
        await CreateFilaProcessamentoIndexesAsync();
    }

    private async Task CreateUserIndexesAsync()
    {
        var collection = _database.GetCollection<User>("users");
        var indexKeys = Builders<User>.IndexKeys.Ascending(u => u.Email);
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<User>(indexKeys, new CreateIndexOptions { Unique = true }));
    }

    private async Task CreateEstadoIndexesAsync()
    {
        var collection = _database.GetCollection<Estado>("estados");
        var indexKeys = Builders<Estado>.IndexKeys.Ascending(e => e.Sigla);
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<Estado>(indexKeys, new CreateIndexOptions { Unique = true }));
    }

    private async Task CreateMunicipioIndexesAsync()
    {
        var collection = _database.GetCollection<Municipio>("municipios");

        var codigoIndex = Builders<Municipio>.IndexKeys.Ascending(m => m.CodigoIbge);
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<Municipio>(codigoIndex, new CreateIndexOptions { Unique = true }));

        var ufIndex = Builders<Municipio>.IndexKeys.Ascending(m => m.SiglaEstado);
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<Municipio>(ufIndex));
    }

    private async Task CreateServicoIndexesAsync()
    {
        var collection = _database.GetCollection<Servico>("servicos");
        var codigoIndex = Builders<Servico>.IndexKeys.Ascending(s => s.CodigoTribNac);
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<Servico>(codigoIndex, new CreateIndexOptions { Unique = true }));
    }

    private async Task CreateAliquotaIndexesAsync()
    {
        var collection = _database.GetCollection<Aliquota>("aliquotas");

        var municipioIndex = Builders<Aliquota>.IndexKeys.Ascending(a => a.CodigoMunicipio);
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<Aliquota>(municipioIndex));

        var compostoIndex = Builders<Aliquota>.IndexKeys
            .Ascending(a => a.CodigoMunicipio)
            .Ascending(a => a.CodigoServico);
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<Aliquota>(compostoIndex, new CreateIndexOptions { Unique = true }));

        var competenciaIndex = Builders<Aliquota>.IndexKeys
            .Ascending(a => a.CodigoMunicipio)
            .Ascending(a => a.CodigoServico)
            .Ascending(a => a.Competencia);
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<Aliquota>(competenciaIndex));
    }

    private async Task CreateExecucaoCrawlerIndexesAsync()
    {
        var collection = _database.GetCollection<ExecucaoCrawler>("execucoesCrawler");

        var inicioIndex = Builders<ExecucaoCrawler>.IndexKeys.Descending(e => e.Inicio);
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<ExecucaoCrawler>(inicioIndex));
    }

    private async Task CreateFilaProcessamentoIndexesAsync()
    {
        var collection = _database.GetCollection<FilaProcessamento>("filaProcessamento");

        var uniqueIndex = Builders<FilaProcessamento>.IndexKeys
            .Ascending(f => f.CodigoMunicipio)
            .Ascending(f => f.CodigoServico)
            .Ascending(f => f.Competencia);
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<FilaProcessamento>(uniqueIndex, new CreateIndexOptions { Unique = true }));

        var statusIndex = Builders<FilaProcessamento>.IndexKeys
            .Ascending(f => f.Status)
            .Ascending(f => f.ProximaTentativa);
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<FilaProcessamento>(statusIndex));

        var execucaoIndex = Builders<FilaProcessamento>.IndexKeys
            .Ascending(f => f.ExecucaoId);
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<FilaProcessamento>(execucaoIndex));
    }
}
