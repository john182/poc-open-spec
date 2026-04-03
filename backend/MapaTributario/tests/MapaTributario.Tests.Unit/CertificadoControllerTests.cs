#pragma warning disable CS8604 // ToString() em objetos anônimos retorna string? mas nunca é null nos testes
using System.Security.Cryptography;
using MapaTributario.API.Application.Crawler;
using MapaTributario.API.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class CertificadoControllerTests
{
    private readonly Mock<ICertificadoStore> _certificadoStore = new();
    private readonly Mock<ILogger<CertificadoController>> _logger = new();

    private CertificadoController CriarSut()
    {
        return new CertificadoController(_certificadoStore.Object, _logger.Object);
    }

    private static IFormFile CriarFormFileFalso(byte[]? conteudo = null, string nomeArquivo = "certificado.pfx")
    {
        byte[] dados = conteudo ?? [0x01, 0x02, 0x03];
        MemoryStream stream = new(dados);
        Mock<IFormFile> formFile = new();
        formFile.Setup(f => f.Length).Returns(dados.Length);
        formFile.Setup(f => f.FileName).Returns(nomeArquivo);
        formFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns((Stream destino, CancellationToken _) =>
            {
                stream.Position = 0;
                return stream.CopyToAsync(destino);
            });
        return formFile.Object;
    }

    // ── Upload ──────────────────────────────────────────────────────

    [Fact]
    public async Task Given_ArquivoNulo_Should_RetornarBadRequest()
    {
        // Arrange
        CertificadoController sut = CriarSut();

        // Act
        IActionResult resultado = await sut.Upload(null!, "senha123");

        // Assert
        BadRequestObjectResult badRequest = resultado.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value!.ToString().ShouldContain("Arquivo PFX e obrigatorio");
    }

    [Fact]
    public async Task Given_ArquivoVazio_Should_RetornarBadRequest()
    {
        // Arrange
        CertificadoController sut = CriarSut();
        Mock<IFormFile> formFileVazio = new();
        formFileVazio.Setup(f => f.Length).Returns(0);

        // Act
        IActionResult resultado = await sut.Upload(formFileVazio.Object, "senha123");

        // Assert
        BadRequestObjectResult badRequest = resultado.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value!.ToString().ShouldContain("Arquivo PFX e obrigatorio");
    }

    [Fact]
    public async Task Given_SenhaNula_Should_RetornarBadRequest()
    {
        // Arrange
        CertificadoController sut = CriarSut();
        IFormFile arquivo = CriarFormFileFalso();

        // Act
        IActionResult resultado = await sut.Upload(arquivo, null!);

        // Assert
        BadRequestObjectResult badRequest = resultado.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value!.ToString().ShouldContain("Senha do certificado e obrigatoria");
    }

    [Fact]
    public async Task Given_SenhaVazia_Should_RetornarBadRequest()
    {
        // Arrange
        CertificadoController sut = CriarSut();
        IFormFile arquivo = CriarFormFileFalso();

        // Act
        IActionResult resultado = await sut.Upload(arquivo, "   ");

        // Assert
        BadRequestObjectResult badRequest = resultado.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value!.ToString().ShouldContain("Senha do certificado e obrigatoria");
    }

    [Fact]
    public async Task Given_PfxValido_Should_RetornarOkComMensagem()
    {
        // Arrange
        CertificadoController sut = CriarSut();
        IFormFile arquivo = CriarFormFileFalso();
        _certificadoStore.Setup(s => s.StoreAsync(It.IsAny<byte[]>(), "senha123"))
            .Returns(Task.CompletedTask);

        // Act
        IActionResult resultado = await sut.Upload(arquivo, "senha123");

        // Assert
        OkObjectResult ok = resultado.ShouldBeOfType<OkObjectResult>();
        ok.Value!.ToString().ShouldContain("Certificado armazenado com sucesso");
        _certificadoStore.Verify(s => s.StoreAsync(It.IsAny<byte[]>(), "senha123"), Times.Once);
    }

    [Fact]
    public async Task Given_PfxInvalidoOuSenhaIncorreta_Should_RetornarBadRequest()
    {
        // Arrange
        CertificadoController sut = CriarSut();
        IFormFile arquivo = CriarFormFileFalso();
        _certificadoStore.Setup(s => s.StoreAsync(It.IsAny<byte[]>(), It.IsAny<string>()))
            .ThrowsAsync(new CryptographicException("Invalid PFX data"));

        // Act
        IActionResult resultado = await sut.Upload(arquivo, "senha-errada");

        // Assert
        BadRequestObjectResult badRequest = resultado.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value!.ToString().ShouldContain("Arquivo PFX invalido ou senha incorreta");
    }

    // ── Status ──────────────────────────────────────────────────────

    [Fact]
    public void Given_SemCertificado_Should_RetornarStatusFalso()
    {
        // Arrange
        CertificadoController sut = CriarSut();
        _certificadoStore.Setup(s => s.HasCertificate()).Returns(false);
        _certificadoStore.Setup(s => s.UploadedAt).Returns((DateTime?)null);
        _certificadoStore.Setup(s => s.Thumbprint).Returns((string?)null);
        _certificadoStore.Setup(s => s.Subject).Returns((string?)null);
        _certificadoStore.Setup(s => s.ValidoAte).Returns((DateTime?)null);

        // Act
        IActionResult resultado = sut.Status();

        // Assert
        OkObjectResult ok = resultado.ShouldBeOfType<OkObjectResult>();
        API.Application.Crawler.Contracts.CertificadoStatusResponse status =
            ok.Value.ShouldBeOfType<API.Application.Crawler.Contracts.CertificadoStatusResponse>();
        status.HasCertificate.ShouldBeFalse();
        status.UploadedAt.ShouldBeNull();
        status.Thumbprint.ShouldBeNull();
        status.Subject.ShouldBeNull();
        status.ValidoAte.ShouldBeNull();
    }

    [Fact]
    public void Given_ComCertificado_Should_RetornarStatusVerdadeiroComMetadados()
    {
        // Arrange
        CertificadoController sut = CriarSut();
        DateTime dataUpload = new(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);
        DateTime validoAte = new(2027, 4, 1, 12, 0, 0, DateTimeKind.Utc);
        _certificadoStore.Setup(s => s.HasCertificate()).Returns(true);
        _certificadoStore.Setup(s => s.UploadedAt).Returns(dataUpload);
        _certificadoStore.Setup(s => s.Thumbprint).Returns("ABC123");
        _certificadoStore.Setup(s => s.Subject).Returns("CN=Teste");
        _certificadoStore.Setup(s => s.ValidoAte).Returns(validoAte);

        // Act
        IActionResult resultado = sut.Status();

        // Assert
        OkObjectResult ok = resultado.ShouldBeOfType<OkObjectResult>();
        API.Application.Crawler.Contracts.CertificadoStatusResponse status =
            ok.Value.ShouldBeOfType<API.Application.Crawler.Contracts.CertificadoStatusResponse>();
        status.HasCertificate.ShouldBeTrue();
        status.UploadedAt.ShouldBe(dataUpload);
        status.Thumbprint.ShouldBe("ABC123");
        status.Subject.ShouldBe("CN=Teste");
        status.ValidoAte.ShouldBe(validoAte);
    }

    // ── Remove ──────────────────────────────────────────────────────

    [Fact]
    public async Task Given_CertificadoCarregado_Should_RemoverComSucesso()
    {
        // Arrange
        CertificadoController sut = CriarSut();
        _certificadoStore.Setup(s => s.RemoveAsync()).Returns(Task.CompletedTask);

        // Act
        IActionResult resultado = await sut.Remove();

        // Assert
        OkObjectResult ok = resultado.ShouldBeOfType<OkObjectResult>();
        ok.Value!.ToString().ShouldContain("Certificado removido com sucesso");
        _certificadoStore.Verify(s => s.RemoveAsync(), Times.Once);
    }
}
