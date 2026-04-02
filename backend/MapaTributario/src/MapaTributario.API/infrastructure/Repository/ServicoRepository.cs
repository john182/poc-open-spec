using System.Text.RegularExpressions;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using MongoDB.Driver;

namespace MapaTributario.API.Infrastructure.Repository;

public class ServicoRepository : IServicoRepository
{
    private readonly IMongoCollection<Servico> _servicos;

    public ServicoRepository(IMongoDatabase database)
    {
        _servicos = database.GetCollection<Servico>("servicos");
    }

    public async Task<IReadOnlyList<Servico>> GetAllAsync()
    {
        return await _servicos
            .Find(FilterDefinition<Servico>.Empty)
            .SortBy(s => s.CodigoTribNac)
            .ToListAsync();
    }

    public async Task<Servico?> GetByCodigoAsync(string codigoTribNac)
    {
        return await _servicos
            .Find(s => s.CodigoTribNac == codigoTribNac)
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyDictionary<string, string>> ObterDescricoesPorCodigosAsync(IEnumerable<string> codigosTribNac)
    {
        var codigos = codigosTribNac.ToList();
        if (codigos.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        var filter = Builders<Servico>.Filter.In(s => s.CodigoTribNac, codigos);
        var servicos = await _servicos.Find(filter).ToListAsync();
        return servicos.ToDictionary(s => s.CodigoTribNac, s => s.Descricao);
    }

    public async Task<IReadOnlyList<string>> BuscarCodigosPorDescricaoAsync(string descricao)
    {
        var escapado = Regex.Escape(descricao);
        var filtro = Builders<Servico>.Filter.Regex(s => s.Descricao, new MongoDB.Bson.BsonRegularExpression(escapado, "i"));
        var servicos = await _servicos.Find(filtro)
            .Project(s => s.CodigoTribNac)
            .ToListAsync();
        return servicos;
    }

    public async Task<long> CountAsync()
    {
        return await _servicos.CountDocumentsAsync(FilterDefinition<Servico>.Empty);
    }

    public async Task InsertManyAsync(IEnumerable<Servico> servicos)
    {
        var list = servicos.ToList();
        if (list.Count > 0)
        {
            await _servicos.InsertManyAsync(list);
        }
    }
}
