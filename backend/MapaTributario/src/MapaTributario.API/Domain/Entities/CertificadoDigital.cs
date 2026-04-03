namespace MapaTributario.API.Domain.Entities;

public class CertificadoDigital
{
    public string Id { get; set; } = null!;
    public byte[] PfxBytes { get; set; } = null!;
    public string Senha { get; set; } = null!;
    public string Thumbprint { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public DateTime ValidoAte { get; set; }
    public DateTime DataUpload { get; set; }

    private CertificadoDigital() { }

    public static CertificadoDigital Criar(
        byte[] pfxBytes,
        string senha,
        string thumbprint,
        string subject,
        DateTime validoAte)
    {
        return new CertificadoDigital
        {
            PfxBytes = pfxBytes,
            Senha = senha,
            Thumbprint = thumbprint,
            Subject = subject,
            ValidoAte = validoAte,
            DataUpload = DateTime.UtcNow
        };
    }
}
