using System.Diagnostics.CodeAnalysis;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using MongoDB.Driver;

namespace MapaTributario.API.Infrastructure.Repository;

[ExcludeFromCodeCoverage]
public class CertificadoDigitalRepository : ICertificadoDigitalRepository
{
    private readonly IMongoCollection<CertificadoDigital> _certificados;

    public CertificadoDigitalRepository(IMongoDatabase database)
    {
        _certificados = database.GetCollection<CertificadoDigital>("certificados_digitais");
    }

    public async Task<CertificadoDigital?> ObterAsync()
    {
        return await _certificados
            .Find(_ => true)
            .FirstOrDefaultAsync();
    }

    public async Task SalvarAsync(CertificadoDigital certificado)
    {
        await _certificados.DeleteManyAsync(_ => true);
        await _certificados.InsertOneAsync(certificado);
    }

    public async Task RemoverAsync()
    {
        await _certificados.DeleteManyAsync(_ => true);
    }
}
