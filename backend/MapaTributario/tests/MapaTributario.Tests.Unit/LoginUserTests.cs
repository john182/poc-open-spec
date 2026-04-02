using MapaTributario.API.Application.Auth;
using MapaTributario.API.Application.Auth.Contracts;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using Moq;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class LoginUserTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenProvider> _tokenProvider = new();
    private readonly LoginUser _sut;

    public LoginUserTests()
    {
        _tokenProvider.Setup(x => x.GenerateAccessToken(It.IsAny<User>())).Returns("access-token");
        _tokenProvider.Setup(x => x.GenerateRefreshToken(It.IsAny<User>())).Returns("refresh-token");
        _tokenProvider.Setup(x => x.AccessTokenExpirySeconds).Returns(3600);

        _sut = new LoginUser(_userRepository.Object, _passwordHasher.Object, _tokenProvider.Object);
    }

    [Fact]
    public async Task Execute_ComCredenciaisValidas_RetornaTokens()
    {
        var request = new LoginRequest { Email = "test@test.com", Senha = "password123" };
        var user = User.Create("test@test.com", "Test", "hashed");
        _userRepository.Setup(x => x.GetByEmailAsync(request.Email)).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.Verify("password123", "hashed")).Returns(true);

        var result = await _sut.ExecuteAsync(request);

        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldBe("access-token");
    }

    [Fact]
    public async Task Execute_ComSenhaInvalida_RetornaFalha()
    {
        var request = new LoginRequest { Email = "test@test.com", Senha = "wrong" };
        var user = User.Create("test@test.com", "Test", "hashed");
        _userRepository.Setup(x => x.GetByEmailAsync(request.Email)).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.Verify("wrong", "hashed")).Returns(false);

        var result = await _sut.ExecuteAsync(request);

        result.IsFailed.ShouldBeTrue();
        result.Errors.First().Message.ShouldContain("Credenciais inválidas");
    }

    [Fact]
    public async Task Execute_ComUsuarioInexistente_RetornaFalha()
    {
        var request = new LoginRequest { Email = "notfound@test.com", Senha = "password123" };
        _userRepository.Setup(x => x.GetByEmailAsync(request.Email)).ReturnsAsync((User?)null);

        var result = await _sut.ExecuteAsync(request);

        result.IsFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task Execute_ComContaInativa_RetornaFalha()
    {
        var request = new LoginRequest { Email = "test@test.com", Senha = "password123" };
        var user = User.Create("test@test.com", "Test", "hashed");
        user.Deactivate();
        _userRepository.Setup(x => x.GetByEmailAsync(request.Email)).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.Verify("password123", "hashed")).Returns(true);

        var result = await _sut.ExecuteAsync(request);

        result.IsFailed.ShouldBeTrue();
        result.Errors.First().Message.ShouldContain("Conta inativa");
    }
}
