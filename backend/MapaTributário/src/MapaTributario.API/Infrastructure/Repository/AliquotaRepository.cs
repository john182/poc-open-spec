using System.Text.RegularExpressions;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using MongoDB.Driver;

namespace MapaTributario.API.Infrastructure.Repository;

public class AliquotaRepository : IAliquotaRepository
{
    private readonly IMongoCollection<Aliquota> _aliquotas;

    public AliquotaRepository(IMongoDatabase database)
    {
        _aliquotas = database.GetCollection<Aliquota>("aliquotas");

        var municipioIndex = Builders<Aliquota>.IndexKeys.Ascending(a => a.CodigoMunicipio);
        _aliquotas.Indexes.CreateOne(new CreateIndexModel<Aliquota>(municipioIndex));

        var compostoIndex = Builders<Aliquota>.IndexKeys
            .Ascending(a => a.CodigoMunicipio)
            .Ascending(a => a.CodigoServico);
        _aliquotas.Indexes.CreateOne(
            new CreateIndexModel<Aliquota>(compostoIndex, new CreateIndexOptions { Unique = true }));

        var competenciaIndex = Builders<Aliquota>.IndexKeys
            .Ascending(a => a.CodigoMunicipio)
            .Ascending(a => a.CodigoServico)
            .Ascending(a => a.Competencia);
        _aliquotas.Indexes.CreateOne(new CreateIndexModel<Aliquota>(competenciaIndex));
    }

    public async Task<(IReadOnlyList<Aliquota> Items, long Total)> GetByMunicipioAsync(
        string codigoIbge,
        int pagina,
        int tamanhoPagina,
        string? codigoServico = null,
        string? descricao = null,
        decimal? aliquotaMin = null,
        decimal? aliquotaMax = null,
        string? competencia = null)
    {
        var filterBuilder = Builders<Aliquota>.Filter;
        var filter = filterBuilder.Eq(a => a.CodigoMunicipio, codigoIbge);

        if (!string.IsNullOrWhiteSpace(codigoServico))
        {
            if (codigoServico.Length < 6)
            {
                var escapedPrefix = Regex.Escape(codigoServico);
                filter &= filterBuilder.Regex(a => a.CodigoServico, new MongoDB.Bson.BsonRegularExpression($"^{escapedPrefix}"));
            }
            else
            {
                filter &= filterBuilder.Eq(a => a.CodigoServico, codigoServico);
            }
        }

        if (!string.IsNullOrWhiteSpace(descricao))
        {
            var escapedDescricao = Regex.Escape(descricao);
            filter &= filterBuilder.Regex(a => a.DescricaoServico, new MongoDB.Bson.BsonRegularExpression(escapedDescricao, "i"));
        }

        if (aliquotaMin.HasValue)
        {
            filter &= filterBuilder.Gte(a => a.ValorAliquota, aliquotaMin.Value);
        }

        if (aliquotaMax.HasValue)
        {
            filter &= filterBuilder.Lte(a => a.ValorAliquota, aliquotaMax.Value);
        }

        if (!string.IsNullOrWhiteSpace(competencia))
        {
            filter &= filterBuilder.Eq(a => a.Competencia, competencia);
        }

        var total = await _aliquotas.CountDocumentsAsync(filter);

        var items = await _aliquotas
            .Find(filter)
            .SortBy(a => a.CodigoServico)
            .Skip((pagina - 1) * tamanhoPagina)
            .Limit(tamanhoPagina)
            .ToListAsync();

        return (items, total);
    }

    public async Task<Aliquota?> GetDetalheAsync(string codigoIbge, string codigoServico)
    {
        return await _aliquotas
            .Find(a => a.CodigoMunicipio == codigoIbge && a.CodigoServico == codigoServico)
            .FirstOrDefaultAsync();
    }

    public async Task UpsertAsync(Aliquota aliquota)
    {
        var filter = Builders<Aliquota>.Filter.Eq(a => a.CodigoMunicipio, aliquota.CodigoMunicipio)
            & Builders<Aliquota>.Filter.Eq(a => a.CodigoServico, aliquota.CodigoServico);

        var existing = await _aliquotas.Find(filter).FirstOrDefaultAsync();
        if (existing is null)
        {
            await _aliquotas.InsertOneAsync(aliquota);
        }
        else
        {
            var update = Builders<Aliquota>.Update
                .Set(a => a.ValorAliquota, aliquota.ValorAliquota)
                .Set(a => a.Competencia, aliquota.Competencia)
                .Set(a => a.ColetadoEm, aliquota.ColetadoEm)
                .Set(a => a.DescricaoServico, aliquota.DescricaoServico)
                .Set(a => a.CodigoServicoFormatado, aliquota.CodigoServicoFormatado)
                .Set(a => a.NomeMunicipio, aliquota.NomeMunicipio);

            await _aliquotas.UpdateOneAsync(filter, update);
        }
    }

    public async Task<bool> ExistsAsync(string codigoMunicipio, string codigoServico, string competencia)
    {
        var filter = Builders<Aliquota>.Filter.Eq(a => a.CodigoMunicipio, codigoMunicipio)
            & Builders<Aliquota>.Filter.Eq(a => a.CodigoServico, codigoServico)
            & Builders<Aliquota>.Filter.Eq(a => a.Competencia, competencia);

        return await _aliquotas.CountDocumentsAsync(filter) > 0;
    }
}
