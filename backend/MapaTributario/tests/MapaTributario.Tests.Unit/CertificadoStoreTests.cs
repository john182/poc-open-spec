using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using MapaTributario.API.Infrastructure;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class CertificadoStoreTests
{
    private readonly CertificadoStore _sut = new();

    private static (byte[] pfxBytes, string senha) GerarCertificadoTeste()
    {
        using RSA rsa = RSA.Create(2048);
        CertificateRequest requisicao = new("CN=Teste", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using X509Certificate2 certificado = requisicao.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1));
        string senha = "senha-teste-123";
        byte[] pfxBytes = certificado.Export(X509ContentType.Pkcs12, senha);
        return (pfxBytes, senha);
    }

    [Fact]
    public void Given_StoreRecenteCriado_Should_RetornarHasCertificateFalso()
    {
        // Arrange — store recém-criado, sem certificado

        // Act
        bool resultado = _sut.HasCertificate();

        // Assert
        resultado.ShouldBeFalse();
    }

    [Fact]
    public void Given_StoreRecenteCriado_Should_RetornarUploadedAtNulo()
    {
        // Arrange — store recém-criado

        // Act
        DateTime? uploadedAt = _sut.UploadedAt;

        // Assert
        uploadedAt.ShouldBeNull();
    }

    [Fact]
    public void Given_StoreRecenteCriado_Should_RetornarGetCertificateNulo()
    {
        // Arrange — store recém-criado

        // Act
        X509Certificate2? certificado = _sut.GetCertificate();

        // Assert
        certificado.ShouldBeNull();
    }

    [Fact]
    public async Task Given_PfxValido_Should_ArmazenarComSucesso()
    {
        // Arrange
        (byte[] pfxBytes, string senha) = GerarCertificadoTeste();

        // Act
        await _sut.StoreAsync(pfxBytes, senha);

        // Assert
        _sut.HasCertificate().ShouldBeTrue();
    }

    [Fact]
    public async Task Given_PfxValido_Should_DefinirUploadedAt()
    {
        // Arrange
        (byte[] pfxBytes, string senha) = GerarCertificadoTeste();
        DateTime antesDaOperacao = DateTime.UtcNow;

        // Act
        await _sut.StoreAsync(pfxBytes, senha);

        // Assert
        _sut.UploadedAt.ShouldNotBeNull();
        _sut.UploadedAt!.Value.ShouldBeGreaterThanOrEqualTo(antesDaOperacao);
        _sut.UploadedAt!.Value.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Fact]
    public async Task Given_PfxValido_Should_RetornarCertificadoViaGetCertificate()
    {
        // Arrange
        (byte[] pfxBytes, string senha) = GerarCertificadoTeste();

        // Act
        await _sut.StoreAsync(pfxBytes, senha);
        X509Certificate2? certificado = _sut.GetCertificate();

        // Assert
        certificado.ShouldNotBeNull();
        certificado.Subject.ShouldContain("CN=Teste");
    }

    [Fact]
    public async Task Given_CertificadoArmazenado_Should_RemoverComSucesso()
    {
        // Arrange
        (byte[] pfxBytes, string senha) = GerarCertificadoTeste();
        await _sut.StoreAsync(pfxBytes, senha);

        // Act
        _sut.Remove();

        // Assert
        _sut.HasCertificate().ShouldBeFalse();
        _sut.UploadedAt.ShouldBeNull();
        _sut.GetCertificate().ShouldBeNull();
    }

    [Fact]
    public async Task Given_PfxInvalido_Should_LancarCryptographicException()
    {
        // Arrange
        byte[] bytesInvalidos = [0x00, 0x01, 0x02, 0x03];
        string senha = "qualquer";

        // Act & Assert
        await Should.ThrowAsync<CryptographicException>(
            () => _sut.StoreAsync(bytesInvalidos, senha));
    }

    [Fact]
    public async Task Given_SenhaIncorreta_Should_LancarCryptographicException()
    {
        // Arrange
        (byte[] pfxBytes, _) = GerarCertificadoTeste();
        string senhaErrada = "senha-errada";

        // Act & Assert
        await Should.ThrowAsync<CryptographicException>(
            () => _sut.StoreAsync(pfxBytes, senhaErrada));
    }

    [Fact]
    public async Task Given_SegundoStore_Should_SubstituirCertificadoAnterior()
    {
        // Arrange
        (byte[] pfxBytes1, string senha1) = GerarCertificadoTeste();
        await _sut.StoreAsync(pfxBytes1, senha1);
        DateTime? primeiroUpload = _sut.UploadedAt;

        // Gerar segundo certificado
        using RSA rsa = RSA.Create(2048);
        CertificateRequest requisicao = new("CN=Segundo", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using X509Certificate2 cert2 = requisicao.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1));
        byte[] pfxBytes2 = cert2.Export(X509ContentType.Pkcs12, "outra-senha");

        // Act
        await _sut.StoreAsync(pfxBytes2, "outra-senha");

        // Assert
        _sut.HasCertificate().ShouldBeTrue();
        X509Certificate2? certificado = _sut.GetCertificate();
        certificado.ShouldNotBeNull();
        certificado.Subject.ShouldContain("CN=Segundo");
        _sut.UploadedAt.ShouldNotBeNull();
        _sut.UploadedAt!.Value.ShouldBeGreaterThanOrEqualTo(primeiroUpload!.Value);
    }

    [Fact]
    public void Given_RemoveSemCertificado_Should_NaoLancarExcecao()
    {
        // Arrange — store vazio

        // Act & Assert — não deve lançar exceção
        Should.NotThrow(() => _sut.Remove());
    }
}
