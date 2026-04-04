using System.Security.Cryptography.X509Certificates;

namespace MapaTributario.API.Application.Crawler;

public interface ICertificadoStore
{
    Task StoreAsync(byte[] pfxBytes, string password);
    X509Certificate2? GetCertificate();
    bool HasCertificate();
    Task RemoveAsync();
    Task CarregarDoBancoAsync();
    DateTime? UploadedAt { get; }
    string? Thumbprint { get; }
    string? Subject { get; }
    DateTime? ValidoAte { get; }
}
