using System.Security.Cryptography.X509Certificates;
using MapaTributario.API.Application.Crawler;

namespace MapaTributario.API.Infrastructure;

public class CertificadoStore : ICertificadoStore
{
    private readonly object _lock = new();
    private byte[]? _pfxBytes;
    private string? _password;
    private X509Certificate2? _certificate;
    private DateTime? _uploadedAt;

    public DateTime? UploadedAt
    {
        get { lock (_lock) { return _uploadedAt; } }
    }

    public Task StoreAsync(byte[] pfxBytes, string password)
    {
        lock (_lock)
        {
            _certificate?.Dispose();
            _pfxBytes = pfxBytes;
            _password = password;
            _certificate = X509CertificateLoader.LoadPkcs12(pfxBytes, password);
            _uploadedAt = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    public X509Certificate2? GetCertificate()
    {
        lock (_lock)
        {
            if (_certificate is null && _pfxBytes is not null)
            {
                _certificate = X509CertificateLoader.LoadPkcs12(_pfxBytes, _password);
            }

            return _certificate;
        }
    }

    public bool HasCertificate()
    {
        lock (_lock)
        {
            return _pfxBytes is not null;
        }
    }

    public void Remove()
    {
        lock (_lock)
        {
            _certificate?.Dispose();
            _certificate = null;
            _pfxBytes = null;
            _password = null;
            _uploadedAt = null;
        }
    }
}
