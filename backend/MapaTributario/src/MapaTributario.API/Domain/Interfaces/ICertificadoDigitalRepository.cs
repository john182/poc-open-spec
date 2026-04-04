using MapaTributario.API.Domain.Entities;

namespace MapaTributario.API.Domain.Interfaces;

public interface ICertificadoDigitalRepository
{
    Task<CertificadoDigital?> ObterAsync();
    Task SalvarAsync(CertificadoDigital certificado);
    Task RemoverAsync();
}
