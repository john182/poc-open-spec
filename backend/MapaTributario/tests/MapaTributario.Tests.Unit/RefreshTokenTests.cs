using MapaTributario.API.Application.Auth;
using MapaTributario.API.Application.Auth.Contracts;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using Moq;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class RefreshTokenTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ITokenProvider> _tokenProvider = new();
    private readonly RefreshToken _sut;

    public RefreshTokenTests()
    {
        _tokenProvider.Setup(x => x.GenerateAccessToken(It.IsAny<User>())).Returns("new-access");
        _tokenProvider.Setup(x => x.GenerateRefreshToken(It.IsAny<User>())).Returns("new-refresh");
        _tokenProvider.Setup(x => x.AccessTokenExpirySeconds).Returns(3600);

        _sut = new RefreshToken(_userRepository.Object, _tokenProvider.Object);
    }

    [Fact]
    public async Task Execute_ComTokenValido_RetornaNovosTokens()
    {
        var request = new RefreshRequest { RefreshToken = "valid-token" };
        var user = User.Create("test@test.com", "Test", "hash");
        _tokenProvider.Setup(x => x.GetUserIdFromToken("valid-token")).Returns("user-id");
        _userRepository.Setup(x => x.GetByIdAsync("user-id")).ReturnsAsync(user);

        var result = await _sut.ExecuteAsync(request);

        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldBe("new-access");
    }

    [Fact]
    public async Task Execute_ComTokenInvalido_RetornaFalha()
    {
        var request = new RefreshRequest { RefreshToken = "invalid" };
        _tokenProvider.Setup(x => x.GetUserIdFromToken("invalid")).Returns((string?)null);

        var result = await _sut.ExecuteAsync(request);

        result.IsFailed.ShouldBeTrue();
        result.Errors.First().Message.ShouldContain("Token inválido");
    }

    [Fact]
    public async Task Execute_ComUsuarioInativo_RetornaFalha()
    {
        var request = new RefreshRequest { RefreshToken = "valid-token" };
        var user = User.Create("test@test.com", "Test", "hash");
        user.Deactivate();
        _tokenProvider.Setup(x => x.GetUserIdFromToken("valid-token")).Returns("user-id");
        _userRepository.Setup(x => x.GetByIdAsync("user-id")).ReturnsAsync(user);

        var result = await _sut.ExecuteAsync(request);

        result.IsFailed.ShouldBeTrue();
    }
}
