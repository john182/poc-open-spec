namespace MapaTributario.API.Application.Crawler.Contracts;

public class CertificadoStatusResponse
{
    public bool HasCertificate { get; set; }
    public DateTime? UploadedAt { get; set; }
}
