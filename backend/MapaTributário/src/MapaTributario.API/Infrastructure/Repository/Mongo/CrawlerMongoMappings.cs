using MapaTributario.API.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;

namespace MapaTributario.API.Infrastructure.Repository.Mongo;

public static class CrawlerMongoMappings
{
    private static bool _registered;

    public static void Register()
    {
        if (_registered)
        {
            return;
        }

        _registered = true;

        RegisterExecucaoCrawler();
        RegisterFilaProcessamento();
    }

    private static void RegisterExecucaoCrawler()
    {
        BsonClassMap.RegisterClassMap<ExecucaoCrawler>(cm =>
        {
            cm.AutoMap();
            cm.MapIdMember(e => e.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(new StringSerializer(BsonType.ObjectId));
            cm.MapMember(e => e.Inicio).SetElementName("inicio");
            cm.MapMember(e => e.Fim).SetElementName("fim");
            cm.MapMember(e => e.Status).SetElementName("status")
                .SetSerializer(new EnumSerializer<StatusExecucao>(BsonType.String));
            cm.MapMember(e => e.Tipo).SetElementName("tipo")
                .SetSerializer(new EnumSerializer<TipoExecucao>(BsonType.String));
            cm.MapMember(e => e.TotalMunicipios).SetElementName("totalMunicipios");
            cm.MapMember(e => e.TotalServicos).SetElementName("totalServicos");
            cm.MapMember(e => e.Processados).SetElementName("processados");
            cm.MapMember(e => e.Erros).SetElementName("erros");
            cm.MapMember(e => e.DetalhesErro).SetElementName("detalhesErro");
            cm.SetIgnoreExtraElements(true);
        });
    }

    private static void RegisterFilaProcessamento()
    {
        BsonClassMap.RegisterClassMap<FilaProcessamento>(cm =>
        {
            cm.AutoMap();
            cm.MapIdMember(f => f.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(new StringSerializer(BsonType.ObjectId));
            cm.MapMember(f => f.CodigoMunicipio).SetElementName("codigoMunicipio");
            cm.MapMember(f => f.CodigoServico).SetElementName("codigoServico");
            cm.MapMember(f => f.Competencia).SetElementName("competencia");
            cm.MapMember(f => f.Status).SetElementName("status")
                .SetSerializer(new EnumSerializer<StatusFila>(BsonType.String));
            cm.MapMember(f => f.Tentativas).SetElementName("tentativas");
            cm.MapMember(f => f.UltimoErro).SetElementName("ultimoErro");
            cm.MapMember(f => f.ProximaTentativa).SetElementName("proximaTentativa");
            cm.MapMember(f => f.ExecucaoId).SetElementName("execucaoId");
            cm.MapMember(f => f.CriadoEm).SetElementName("criadoEm");
            cm.MapMember(f => f.AtualizadoEm).SetElementName("atualizadoEm");
            cm.SetIgnoreExtraElements(true);
        });
    }
}
