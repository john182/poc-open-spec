using System.Security.Cryptography.X509Certificates;

namespace MapaTributario.API.Application.Crawler;

public interface ICertificadoStore
{
    Task StoreAsync(byte[] pfxBytes, string password);
    X509Certificate2? GetCertificate();
    bool HasCertificate();
    void Remove();
    DateTime? UploadedAt { get; }
}
