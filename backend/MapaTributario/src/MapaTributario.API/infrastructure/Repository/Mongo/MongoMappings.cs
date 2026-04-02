using System.Diagnostics.CodeAnalysis;
using MapaTributario.API.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;

namespace MapaTributario.API.Infrastructure.Repository.Mongo;

[ExcludeFromCodeCoverage]
public static class MongoMappings
{
    private static bool _registered;

    public static void Register()
    {
        if (_registered)
        {
            return;
        }

        _registered = true;

        RegisterUser();
        RegisterEstado();
        RegisterMunicipio();
        RegisterServico();
        RegisterAliquota();
    }

    private static void RegisterUser()
    {
        BsonClassMap.RegisterClassMap<User>(cm =>
        {
            cm.AutoMap();
            cm.MapIdMember(u => u.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(new StringSerializer(BsonType.ObjectId));
            cm.MapMember(u => u.Email).SetElementName("email");
            cm.MapMember(u => u.PasswordHash).SetElementName("passwordHash");
            cm.MapMember(u => u.Nome).SetElementName("nome");
            cm.MapMember(u => u.Role).SetElementName("role");
            cm.MapMember(u => u.DataCriacao).SetElementName("dataCriacao");
            cm.MapMember(u => u.Ativo).SetElementName("ativo");
            cm.SetIgnoreExtraElements(true);
        });
    }

    private static void RegisterEstado()
    {
        BsonClassMap.RegisterClassMap<Estado>(cm =>
        {
            cm.AutoMap();
            cm.MapIdMember(e => e.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(new StringSerializer(BsonType.ObjectId));
            cm.MapMember(e => e.Sigla).SetElementName("sigla");
            cm.MapMember(e => e.Nome).SetElementName("nome");
            cm.MapMember(e => e.Regiao).SetElementName("regiao");
            cm.SetIgnoreExtraElements(true);
        });
    }

    private static void RegisterMunicipio()
    {
        BsonClassMap.RegisterClassMap<Municipio>(cm =>
        {
            cm.AutoMap();
            cm.MapIdMember(m => m.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(new StringSerializer(BsonType.ObjectId));
            cm.MapMember(m => m.CodigoIbge).SetElementName("codigoIbge");
            cm.MapMember(m => m.Nome).SetElementName("nome");
            cm.MapMember(m => m.SiglaEstado).SetElementName("siglaEstado");
            cm.MapMember(m => m.EhCapital).SetElementName("ehCapital");
            cm.SetIgnoreExtraElements(true);
        });
    }

    private static void RegisterServico()
    {
        BsonClassMap.RegisterClassMap<Servico>(cm =>
        {
            cm.AutoMap();
            cm.MapIdMember(s => s.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(new StringSerializer(BsonType.ObjectId));
            cm.MapMember(s => s.CodigoTribNac).SetElementName("codigoTribNac");
            cm.MapMember(s => s.Descricao).SetElementName("descricao");
            cm.MapMember(s => s.Item).SetElementName("item");
            cm.MapMember(s => s.Subitem).SetElementName("subitem");
            cm.MapMember(s => s.Desdobramento).SetElementName("desdobramento");
            cm.SetIgnoreExtraElements(true);
        });
    }

    private static void RegisterAliquota()
    {
        BsonClassMap.RegisterClassMap<Aliquota>(cm =>
        {
            cm.AutoMap();
            cm.MapIdMember(a => a.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(new StringSerializer(BsonType.ObjectId));
            cm.MapMember(a => a.CodigoMunicipio).SetElementName("codigoMunicipio");
            cm.MapMember(a => a.NomeMunicipio).SetElementName("nomeMunicipio");
            cm.MapMember(a => a.CodigoServico).SetElementName("codigoServico");
            cm.MapMember(a => a.CodigoServicoFormatado).SetElementName("codigoServicoFormatado");
            cm.MapMember(a => a.DescricaoServico).SetElementName("descricaoServico");
            cm.MapMember(a => a.ValorAliquota).SetElementName("valorAliquota")
                .SetSerializer(new DecimalSerializer(BsonType.Decimal128));
            cm.MapMember(a => a.Competencia).SetElementName("competencia");
            cm.MapMember(a => a.ColetadoEm).SetElementName("coletadoEm");
            cm.MapMember(a => a.Fonte).SetElementName("fonte");
            cm.SetIgnoreExtraElements(true);
        });
    }
}
