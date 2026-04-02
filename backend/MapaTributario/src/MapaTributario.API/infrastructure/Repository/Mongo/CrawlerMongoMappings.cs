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
        RegisterConfiguracaoCrawler();
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
            cm.MapMember(e => e.UfsProcessadas).SetElementName("ufsProcessadas");
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

    private static void RegisterConfiguracaoCrawler()
    {
        BsonClassMap.RegisterClassMap<ConfiguracaoCrawler>(cm =>
        {
            cm.AutoMap();
            cm.MapIdMember(c => c.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(new StringSerializer(BsonType.ObjectId));
            cm.MapMember(c => c.CronSchedule).SetElementName("cronSchedule");
            cm.MapMember(c => c.LimiteRequisicoesPorSegundo).SetElementName("limiteRequisicoesPorSegundo");
            cm.MapMember(c => c.OrcamentoDiario).SetElementName("orcamentoDiario");
            cm.MapMember(c => c.TamanheLoteCertificado).SetElementName("tamanheLoteCertificado");
            cm.MapMember(c => c.PausaLoteSegundos).SetElementName("pausaLoteSegundos");
            cm.MapMember(c => c.TamanheLoteMongo).SetElementName("tamanheLoteMongo");
            cm.MapMember(c => c.MaxTentativas).SetElementName("maxTentativas");
            cm.MapMember(c => c.LimiteParadaAntecipada).SetElementName("limiteParadaAntecipada");
            cm.MapMember(c => c.MaxDesdobramento).SetElementName("maxDesdobramento");
            cm.MapMember(c => c.MaxDetalhamento).SetElementName("maxDetalhamento");
            cm.MapMember(c => c.MaxFalhasConsecutivasDetalhamento).SetElementName("maxFalhasConsecutivasDetalhamento");
            cm.MapMember(c => c.MaxFalhasConsecutivasDesdobramento).SetElementName("maxFalhasConsecutivasDesdobramento");
            cm.MapMember(c => c.MaxItensParalelos).SetElementName("maxItensParalelos");
            cm.MapMember(c => c.CodigosSondagem).SetElementName("codigosSondagem");
            cm.MapMember(c => c.ValidadeDiasProcessamento).SetElementName("validadeDiasProcessamento");
            cm.MapMember(c => c.CircuitBreakerLimiarErroPercent).SetElementName("circuitBreakerLimiarErroPercent");
            cm.MapMember(c => c.CircuitBreakerJanelaAvaliacaoSegundos).SetElementName("circuitBreakerJanelaAvaliacaoSegundos");
            cm.MapMember(c => c.CircuitBreakerPausaSegundos).SetElementName("circuitBreakerPausaSegundos");
            cm.MapMember(c => c.CircuitBreakerAmostraMinima).SetElementName("circuitBreakerAmostraMinima");
            cm.MapMember(c => c.Ativo).SetElementName("ativo");
            cm.MapMember(c => c.CriadoEm).SetElementName("criadoEm");
            cm.MapMember(c => c.AtualizadoEm).SetElementName("atualizadoEm");
            cm.SetIgnoreExtraElements(true);
        });
    }
}
