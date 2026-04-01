using MapaTributario.API.Application.Auth.Contracts;
using MapaTributario.API.Validators;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class ValidatorTests
{
    [Fact]
    public async Task RegisterValidator_ComDadosValidos_PassaValidacao()
    {
        var validator = new RegisterRequestValidator();
        var request = new RegisterRequest { Email = "test@test.com", Nome = "Test", Senha = "password123" };

        var result = await validator.ValidateAsync(request);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task RegisterValidator_SemEmail_FalhaValidacao()
    {
        var validator = new RegisterRequestValidator();
        var request = new RegisterRequest { Email = "", Nome = "Test", Senha = "password123" };

        var result = await validator.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task RegisterValidator_SenhaCurta_FalhaValidacao()
    {
        var validator = new RegisterRequestValidator();
        var request = new RegisterRequest { Email = "test@test.com", Nome = "Test", Senha = "123" };

        var result = await validator.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Senha");
    }

    [Fact]
    public async Task LoginValidator_ComDadosValidos_PassaValidacao()
    {
        var validator = new LoginRequestValidator();
        var request = new LoginRequest { Email = "test@test.com", Senha = "password123" };

        var result = await validator.ValidateAsync(request);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task LoginValidator_SemSenha_FalhaValidacao()
    {
        var validator = new LoginRequestValidator();
        var request = new LoginRequest { Email = "test@test.com", Senha = "" };

        var result = await validator.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task RefreshValidator_SemToken_FalhaValidacao()
    {
        var validator = new RefreshRequestValidator();
        var request = new RefreshRequest { RefreshToken = "" };

        var result = await validator.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
    }
}
