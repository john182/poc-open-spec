using MapaTributario.API.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;

namespace MapaTributario.API.Infrastructure.Repository.Mongo;

public static class MongoMappings
{
    public static void Register()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(User)))
        {
            return;
        }

        BsonClassMap.RegisterClassMap<User>(cm =>
        {
            cm.AutoMap();
            cm.MapIdMember(u => u.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(new StringSerializer(BsonType.ObjectId));
            cm.MapMember(u => u.Email).SetElementName("email");
            cm.MapMember(u => u.PasswordHash).SetElementName("passwordHash");
            cm.MapMember(u => u.Nome).SetElementName("nome");
            cm.MapMember(u => u.DataCriacao).SetElementName("dataCriacao");
            cm.MapMember(u => u.Ativo).SetElementName("ativo");
            cm.SetIgnoreExtraElements(true);
        });
    }
}
