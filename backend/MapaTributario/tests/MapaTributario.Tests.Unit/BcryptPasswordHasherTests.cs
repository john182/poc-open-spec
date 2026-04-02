using MapaTributario.API.Infrastructure.Auth;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class BcryptPasswordHasherTests
{
    private readonly BcryptPasswordHasher _sut = new();

    [Fact]
    public void Given_SenhaValida_Should_GerarHashDiferenteDaSenhaOriginal()
    {
        // Arrange
        string senha = "MinhaSenhaSegura123!";

        // Act
        string hash = _sut.Hash(senha);

        // Assert
        hash.ShouldNotBeNullOrWhiteSpace();
        hash.ShouldNotBe(senha);
    }

    [Fact]
    public void Given_SenhaCorreta_Should_RetornarVerdadeiroNaVerificacao()
    {
        // Arrange
        string senha = "MinhaSenhaSegura123!";
        string hash = _sut.Hash(senha);

        // Act
        bool resultado = _sut.Verify(senha, hash);

        // Assert
        resultado.ShouldBeTrue();
    }

    [Fact]
    public void Given_SenhaIncorreta_Should_RetornarFalsoNaVerificacao()
    {
        // Arrange
        string senhaOriginal = "MinhaSenhaSegura123!";
        string senhaErrada = "SenhaErrada456!";
        string hash = _sut.Hash(senhaOriginal);

        // Act
        bool resultado = _sut.Verify(senhaErrada, hash);

        // Assert
        resultado.ShouldBeFalse();
    }

    [Fact]
    public void Given_MesmaSenha_Should_GerarHashesDiferentes()
    {
        // Arrange
        string senha = "MinhaSenhaSegura123!";

        // Act
        string hash1 = _sut.Hash(senha);
        string hash2 = _sut.Hash(senha);

        // Assert
        hash1.ShouldNotBe(hash2);
    }
}
