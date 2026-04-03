using System.Security.Cryptography.X509Certificates;
using MapaTributario.API.Application.Crawler;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;

namespace MapaTributario.API.Infrastructure;

public class CertificadoStore : ICertificadoStore
{
    private readonly ICertificadoDigitalRepository _repositorio;
    private readonly object _lock = new();
    private byte[]? _pfxBytes;
    private string? _password;
    private X509Certificate2? _certificate;
    private DateTime? _uploadedAt;
    private string? _thumbprint;
    private string? _subject;
    private DateTime? _validoAte;

    public CertificadoStore(ICertificadoDigitalRepository repositorio)
    {
        _repositorio = repositorio;
    }

    public DateTime? UploadedAt
    {
        get { lock (_lock) { return _uploadedAt; } }
    }

    public string? Thumbprint
    {
        get { lock (_lock) { return _thumbprint; } }
    }

    public string? Subject
    {
        get { lock (_lock) { return _subject; } }
    }

    public DateTime? ValidoAte
    {
        get { lock (_lock) { return _validoAte; } }
    }

    public async Task StoreAsync(byte[] pfxBytes, string password)
    {
        // Parsear PFX e extrair metadados antes de persistir
        X509Certificate2 novoCertificado = X509CertificateLoader.LoadPkcs12(pfxBytes, password);

        string thumbprint = novoCertificado.Thumbprint;
        string subject = novoCertificado.Subject;
        DateTime validoAte = novoCertificado.NotAfter;

        // Persistir no MongoDB
        CertificadoDigital entidade = CertificadoDigital.Criar(
            pfxBytes, password, thumbprint, subject, validoAte);
        await _repositorio.SalvarAsync(entidade);

        // Atualizar cache em RAM
        lock (_lock)
        {
            _certificate?.Dispose();
            _pfxBytes = pfxBytes;
            _password = password;
            _certificate = novoCertificado;
            _uploadedAt = entidade.DataUpload;
            _thumbprint = thumbprint;
            _subject = subject;
            _validoAte = validoAte;
        }
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

    public async Task RemoveAsync()
    {
        await _repositorio.RemoverAsync();

        lock (_lock)
        {
            _certificate?.Dispose();
            _certificate = null;
            _pfxBytes = null;
            _password = null;
            _uploadedAt = null;
            _thumbprint = null;
            _subject = null;
            _validoAte = null;
        }
    }

    public async Task CarregarDoBancoAsync()
    {
        CertificadoDigital? entidade = await _repositorio.ObterAsync();

        if (entidade is null)
        {
            return;
        }

        X509Certificate2 certificado = X509CertificateLoader.LoadPkcs12(entidade.PfxBytes, entidade.Senha);

        lock (_lock)
        {
            _certificate?.Dispose();
            _pfxBytes = entidade.PfxBytes;
            _password = entidade.Senha;
            _certificate = certificado;
            _uploadedAt = entidade.DataUpload;
            _thumbprint = entidade.Thumbprint;
            _subject = entidade.Subject;
            _validoAte = entidade.ValidoAte;
        }
    }
}
