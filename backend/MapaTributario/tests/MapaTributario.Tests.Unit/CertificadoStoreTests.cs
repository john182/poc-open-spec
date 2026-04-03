using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using MapaTributario.API.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class CertificadoStoreTests
{
    private readonly Mock<ICertificadoDigitalRepository> _repositorio = new();
    private readonly Mock<ILogger<CertificadoStore>> _logger = new();
    private readonly CertificadoStore _sut;

    public CertificadoStoreTests()
    {
        _repositorio.Setup(r => r.SalvarAsync(It.IsAny<CertificadoDigital>())).Returns(Task.CompletedTask);
        _repositorio.Setup(r => r.RemoverAsync()).Returns(Task.CompletedTask);
        _repositorio.Setup(r => r.ObterAsync()).ReturnsAsync((CertificadoDigital?)null);
        _sut = new CertificadoStore(_repositorio.Object, _logger.Object);
    }

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
        await _sut.RemoveAsync();

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
    public async Task Given_RemoveSemCertificado_Should_NaoLancarExcecao()
    {
        // Arrange — store vazio

        // Act & Assert — não deve lançar exceção
        await Should.NotThrowAsync(() => _sut.RemoveAsync());
    }

    [Fact]
    public async Task Given_PfxValido_Should_PreencherMetadados()
    {
        // Arrange
        (byte[] pfxBytes, string senha) = GerarCertificadoTeste();

        // Act
        await _sut.StoreAsync(pfxBytes, senha);

        // Assert
        _sut.Thumbprint.ShouldNotBeNullOrEmpty();
        _sut.Subject.ShouldContain("CN=Teste");
        _sut.ValidoAte.ShouldNotBeNull();
        _sut.ValidoAte!.Value.ShouldBeGreaterThan(DateTime.UtcNow);
    }

    [Fact]
    public async Task Given_StoreAsync_Should_PersistirNoRepositorio()
    {
        // Arrange
        (byte[] pfxBytes, string senha) = GerarCertificadoTeste();

        // Act
        await _sut.StoreAsync(pfxBytes, senha);

        // Assert
        _repositorio.Verify(r => r.SalvarAsync(It.Is<CertificadoDigital>(
            c => c.Thumbprint != null && c.Subject.Contains("CN=Teste"))), Times.Once);
    }

    [Fact]
    public async Task Given_RemoveAsync_Should_ChamarRepositorioRemover()
    {
        // Arrange
        (byte[] pfxBytes, string senha) = GerarCertificadoTeste();
        await _sut.StoreAsync(pfxBytes, senha);

        // Act
        await _sut.RemoveAsync();

        // Assert
        _repositorio.Verify(r => r.RemoverAsync(), Times.Once);
        _sut.Thumbprint.ShouldBeNull();
        _sut.Subject.ShouldBeNull();
        _sut.ValidoAte.ShouldBeNull();
    }

    [Fact]
    public async Task Given_DadosNoBanco_CarregarDoBancoAsync_Should_PopularCache()
    {
        // Arrange — gerar certificado e simular retorno do repositório
        (byte[] pfxBytes, string senha) = GerarCertificadoTeste();
        CertificadoDigital entidade = CertificadoDigital.Criar(
            pfxBytes, senha, "THUMB123", "CN=DoBanco", DateTime.UtcNow.AddYears(1));

        _repositorio.Setup(r => r.ObterAsync()).ReturnsAsync(entidade);

        // Act
        await _sut.CarregarDoBancoAsync();

        // Assert
        _sut.HasCertificate().ShouldBeTrue();
        _sut.GetCertificate().ShouldNotBeNull();
        _sut.UploadedAt.ShouldBe(entidade.DataUpload);
        _sut.Thumbprint.ShouldNotBeNullOrEmpty();
        _sut.Subject.ShouldNotBeNullOrEmpty();
        _sut.ValidoAte.ShouldNotBeNull();
    }

    [Fact]
    public async Task Given_BancoVazio_CarregarDoBancoAsync_Should_ManterCacheVazio()
    {
        // Arrange — repositório retorna null (padrão do setup)

        // Act
        await _sut.CarregarDoBancoAsync();

        // Assert
        _sut.HasCertificate().ShouldBeFalse();
        _sut.GetCertificate().ShouldBeNull();
        _sut.UploadedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Given_CacheJaPopulado_CarregarDoBancoAsync_Should_SubstituirCertificadoAnterior()
    {
        // Arrange — primeiro carrega um certificado no cache
        (byte[] pfxBytes, string senha) = GerarCertificadoTeste();
        await _sut.StoreAsync(pfxBytes, senha);
        _sut.HasCertificate().ShouldBeTrue();

        // Gerar segundo certificado para retornar do banco
        using RSA rsa = RSA.Create(2048);
        CertificateRequest requisicao = new("CN=DoBancoSegundo", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using X509Certificate2 cert2 = requisicao.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(2));
        byte[] pfxBytes2 = cert2.Export(X509ContentType.Pkcs12, "outra-senha");
        CertificadoDigital entidade = CertificadoDigital.Criar(
            pfxBytes2, "outra-senha", "NEWTHUMB", "CN=DoBancoSegundo", DateTime.UtcNow.AddYears(2));

        _repositorio.Setup(r => r.ObterAsync()).ReturnsAsync(entidade);

        // Act — recarregar do banco com cache já populado (dispõe certificado anterior)
        await _sut.CarregarDoBancoAsync();

        // Assert
        _sut.HasCertificate().ShouldBeTrue();
        _sut.GetCertificate().ShouldNotBeNull();
        _sut.GetCertificate()!.Subject.ShouldContain("CN=DoBancoSegundo");
        _sut.UploadedAt.ShouldBe(entidade.DataUpload);
    }

    [Fact]
    public async Task Given_PfxCorrompidoNoBanco_CarregarDoBancoAsync_Should_ManterCacheVazioELogarErro()
    {
        // Arrange — simular certificado corrompido armazenado no banco
        byte[] bytesInvalidos = [0x00, 0x01, 0x02, 0x03];
        CertificadoDigital entidadeCorrompida = CertificadoDigital.Criar(
            bytesInvalidos, "qualquer", "THUMB", "CN=Corrompido", DateTime.UtcNow.AddYears(1));

        _repositorio.Setup(r => r.ObterAsync()).ReturnsAsync(entidadeCorrompida);

        // Act
        await _sut.CarregarDoBancoAsync();

        // Assert — cache deve permanecer vazio
        _sut.HasCertificate().ShouldBeFalse();
        _sut.GetCertificate().ShouldBeNull();
        _sut.UploadedAt.ShouldBeNull();
        _sut.Thumbprint.ShouldBeNull();
        _sut.Subject.ShouldBeNull();
        _sut.ValidoAte.ShouldBeNull();

        // Verificar que o erro foi logado
        _logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<CryptographicException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
