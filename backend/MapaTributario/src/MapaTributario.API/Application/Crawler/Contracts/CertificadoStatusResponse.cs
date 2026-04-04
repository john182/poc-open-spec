namespace MapaTributario.API.Application.Crawler.Contracts;

public class CertificadoStatusResponse
{
    public bool HasCertificate { get; set; }
    public DateTime? UploadedAt { get; set; }
    public string? Thumbprint { get; set; }
    public string? Subject { get; set; }
    public DateTime? ValidoAte { get; set; }
}
