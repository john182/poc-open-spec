using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class JwtTokenProviderTests
{
    private const string SegredoJwt = "ChaveSecretaSuperSeguraParaTestesUnitarios2026!@#$%";
    private const string Emissor = "teste-emissor";
    private const string Audiencia = "teste-audiencia";

    private static JwtTokenProvider CriarSut(
        string segredo = SegredoJwt,
        string expiryMinutes = "60",
        string refreshExpiryDays = "7",
        string emissor = Emissor,
        string audiencia = Audiencia)
    {
        var configDados = new Dictionary<string, string?>
        {
            ["JWT:Secret"] = segredo,
            ["JWT:ExpiryMinutes"] = expiryMinutes,
            ["JWT:RefreshExpiryDays"] = refreshExpiryDays,
            ["JWT:Issuer"] = emissor,
            ["JWT:Audience"] = audiencia
        };

        IConfiguration configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(configDados)
            .Build();

        return new JwtTokenProvider(configuracao);
    }

    private static User CriarUsuarioValido()
    {
        User usuario = User.Create("usuario@teste.com", "Usuario Teste", "hash123", "Admin");
        usuario.SetId("id-usuario-123");
        return usuario;
    }

    [Fact]
    public void Given_UsuarioValido_Should_GerarAccessTokenNaoVazio()
    {
        // Arrange
        JwtTokenProvider sut = CriarSut();
        User usuario = CriarUsuarioValido();

        // Act
        string token = sut.GenerateAccessToken(usuario);

        // Assert
        token.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Given_UsuarioValido_Should_GerarRefreshTokenNaoVazio()
    {
        // Arrange
        JwtTokenProvider sut = CriarSut();
        User usuario = CriarUsuarioValido();

        // Act
        string token = sut.GenerateRefreshToken(usuario);

        // Assert
        token.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Given_TokenValido_Should_RetornarIdDoUsuario()
    {
        // Arrange
        JwtTokenProvider sut = CriarSut();
        User usuario = CriarUsuarioValido();
        string token = sut.GenerateAccessToken(usuario);

        // Act
        string? idExtraido = sut.GetUserIdFromToken(token);

        // Assert
        idExtraido.ShouldNotBeNull();
        idExtraido.ShouldBe("id-usuario-123");
    }

    [Fact]
    public void Given_TokenInvalido_Should_RetornarNulo()
    {
        // Arrange
        JwtTokenProvider sut = CriarSut();
        string tokenInvalido = "token.completamente.invalido";

        // Act
        string? resultado = sut.GetUserIdFromToken(tokenInvalido);

        // Assert
        resultado.ShouldBeNull();
    }

    [Fact]
    public void Given_TokenGerado_Should_ConterClaimsEsperados()
    {
        // Arrange
        JwtTokenProvider sut = CriarSut();
        User usuario = CriarUsuarioValido();
        string token = sut.GenerateAccessToken(usuario);

        // Act
        JwtSecurityTokenHandler handler = new();
        JwtSecurityToken jwtToken = handler.ReadJwtToken(token);

        // Assert
        jwtToken.Issuer.ShouldBe(Emissor);
        jwtToken.Audiences.ShouldContain(Audiencia);

        string? claimId = jwtToken.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        claimId.ShouldBe("id-usuario-123");

        string? claimRole = jwtToken.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
        claimRole.ShouldBe("Admin");

        string? claimEmail = jwtToken.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        claimEmail.ShouldBe("usuario@teste.com");

        string? claimNome = jwtToken.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        claimNome.ShouldBe("Usuario Teste");
    }

    [Fact]
    public void Given_SemSegredoConfigurado_Should_LancarExcecao()
    {
        // Arrange
        var configDados = new Dictionary<string, string?>();
        IConfiguration configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(configDados)
            .Build();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => new JwtTokenProvider(configuracao));
    }
}
