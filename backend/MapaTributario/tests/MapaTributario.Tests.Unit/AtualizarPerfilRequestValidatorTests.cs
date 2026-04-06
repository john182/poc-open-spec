using MapaTributario.API.Application.Perfil.Contracts;
using MapaTributario.API.Validators;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class AtualizarPerfilRequestValidatorTests
{
    private readonly AtualizarPerfilRequestValidator _sut = new();

    [Fact]
    public async Task Given_NomeValidoSemSenhas_Should_PassarValidacao()
    {
        // Arrange
        var request = new AtualizarPerfilRequest { Nome = "João Silva" };

        // Act
        var resultado = await _sut.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Given_NomeVazio_Should_FalharValidacao()
    {
        // Arrange
        var request = new AtualizarPerfilRequest { Nome = "" };

        // Act
        var resultado = await _sut.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "Nome");
    }

    [Fact]
    public async Task Given_NomeCurtoComUmCaractere_Should_FalharValidacao()
    {
        // Arrange
        var request = new AtualizarPerfilRequest { Nome = "A" };

        // Act
        var resultado = await _sut.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "Nome" && e.ErrorMessage.Contains("mínimo 2 caracteres"));
    }

    [Fact]
    public async Task Given_NovaSenhaPreenchidaSemSenhaAtual_Should_FalharValidacao()
    {
        // Arrange
        var request = new AtualizarPerfilRequest
        {
            Nome = "João Silva",
            NovaSenha = "novaSenha123",
            SenhaAtual = null
        };

        // Act
        var resultado = await _sut.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "SenhaAtual");
    }

    [Fact]
    public async Task Given_NovaSenhaCurtaMenosDeOitoCaracteres_Should_FalharValidacao()
    {
        // Arrange
        var request = new AtualizarPerfilRequest
        {
            Nome = "João Silva",
            SenhaAtual = "senhaAtual123",
            NovaSenha = "curta"
        };

        // Act
        var resultado = await _sut.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "NovaSenha" && e.ErrorMessage.Contains("mínimo 8 caracteres"));
    }

    [Fact]
    public async Task Given_NovaSenhaESenhaAtualValidos_Should_PassarValidacao()
    {
        // Arrange
        var request = new AtualizarPerfilRequest
        {
            Nome = "João Silva",
            SenhaAtual = "senhaAtual123",
            NovaSenha = "novaSenha123"
        };

        // Act
        var resultado = await _sut.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Given_SemNovaSenhaSemSenhaAtual_Should_PassarValidacao()
    {
        // Arrange
        var request = new AtualizarPerfilRequest
        {
            Nome = "Maria Souza",
            SenhaAtual = null,
            NovaSenha = null
        };

        // Act
        var resultado = await _sut.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }
}
