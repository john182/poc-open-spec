using MapaTributario.API.Application.Auth;
using MapaTributario.API.Application.Auth.Contracts;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using Moq;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class RegisterUserTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenProvider> _tokenProvider = new();
    private readonly RegisterUser _sut;

    public RegisterUserTests()
    {
        _passwordHasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("hashed");
        _tokenProvider.Setup(x => x.GenerateAccessToken(It.IsAny<User>())).Returns("access-token");
        _tokenProvider.Setup(x => x.GenerateRefreshToken(It.IsAny<User>())).Returns("refresh-token");
        _tokenProvider.Setup(x => x.AccessTokenExpirySeconds).Returns(3600);

        _sut = new RegisterUser(_userRepository.Object, _passwordHasher.Object, _tokenProvider.Object);
    }

    [Fact]
    public async Task Execute_ComDadosValidos_RetornaTokens()
    {
        var request = new RegisterRequest { Email = "test@test.com", Nome = "Test", Senha = "password123" };
        _userRepository.Setup(x => x.GetByEmailAsync(request.Email)).ReturnsAsync((User?)null);
        _userRepository.Setup(x => x.CreateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var result = await _sut.ExecuteAsync(request);

        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldBe("access-token");
        result.Value.RefreshToken.ShouldBe("refresh-token");
        result.Value.ExpiresIn.ShouldBe(3600);
    }

    [Fact]
    public async Task Execute_ComEmailDuplicado_RetornaFalha()
    {
        var request = new RegisterRequest { Email = "existing@test.com", Nome = "Test", Senha = "password123" };
        _userRepository.Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync(User.Create("existing@test.com", "Existing", "hash"));

        var result = await _sut.ExecuteAsync(request);

        result.IsFailed.ShouldBeTrue();
        result.Errors.First().Message.ShouldContain("Email já cadastrado");
    }
}
