using MapaTributario.API.Application.Crawler;
using MapaTributario.API.Application.Crawler.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapaTributario.API.Controllers;

[ApiController]
[Route("api/v1/crawler/certificado")]
[Authorize(Roles = "Admin")]
public class CertificadoController : ControllerBase
{
    private readonly ICertificadoStore _certificadoStore;
    private readonly ILogger<CertificadoController> _logger;

    public CertificadoController(
        ICertificadoStore certificadoStore,
        ILogger<CertificadoController> logger)
    {
        _certificadoStore = certificadoStore;
        _logger = logger;
    }

    /// <summary>
    /// Upload a PFX certificate for NFS-e API authentication.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB max
    public async Task<IActionResult> Upload(
        IFormFile arquivo,
        [FromForm] string senha)
    {
        if (arquivo is null || arquivo.Length == 0)
        {
            return BadRequest(new { erro = "Arquivo PFX e obrigatorio" });
        }

        if (string.IsNullOrWhiteSpace(senha))
        {
            return BadRequest(new { erro = "Senha do certificado e obrigatoria" });
        }

        try
        {
            using MemoryStream ms = new();
            await arquivo.CopyToAsync(ms);
            byte[] pfxBytes = ms.ToArray();

            await _certificadoStore.StoreAsync(pfxBytes, senha);

            _logger.LogInformation("Certificate PFX uploaded successfully");

            return Ok(new { mensagem = "Certificado armazenado com sucesso" });
        }
        catch (System.Security.Cryptography.CryptographicException ex)
        {
            _logger.LogWarning(ex, "Failed to load PFX certificate — invalid file or password");
            return BadRequest(new { erro = "Arquivo PFX invalido ou senha incorreta" });
        }
    }

    /// <summary>
    /// Returns certificate status (whether one is loaded and when it was uploaded).
    /// </summary>
    [HttpGet]
    public IActionResult Status()
    {
        return Ok(new CertificadoStatusResponse
        {
            HasCertificate = _certificadoStore.HasCertificate(),
            UploadedAt = _certificadoStore.UploadedAt,
            Thumbprint = _certificadoStore.Thumbprint,
            Subject = _certificadoStore.Subject,
            ValidoAte = _certificadoStore.ValidoAte
        });
    }

    /// <summary>
    /// Removes the currently loaded certificate.
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Remove()
    {
        await _certificadoStore.RemoveAsync();
        _logger.LogInformation("Certificate PFX removed");
        return Ok(new { mensagem = "Certificado removido com sucesso" });
    }
}
