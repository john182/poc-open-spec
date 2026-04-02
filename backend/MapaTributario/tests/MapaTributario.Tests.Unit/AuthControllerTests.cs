#pragma warning disable CS8604 // ToString() em objetos anônimos retorna string? mas nunca é null nos testes
using FluentValidation;
using FluentValidation.Results;
using MapaTributario.API.Application.Auth;
using MapaTributario.API.Application.Auth.Contracts;
using MapaTributario.API.Controllers;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class AuthControllerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenProvider> _tokenProvider = new();
    private readonly Mock<IValidator<RegisterRequest>> _registerValidator = new();
    private readonly Mock<IValidator<LoginRequest>> _loginValidator = new();
    private readonly Mock<IValidator<RefreshRequest>> _refreshValidator = new();

    public AuthControllerTests()
    {
        _tokenProvider.Setup(x => x.GenerateAccessToken(It.IsAny<User>())).Returns("access-token");
        _tokenProvider.Setup(x => x.GenerateRefreshToken(It.IsAny<User>())).Returns("refresh-token");
        _tokenProvider.Setup(x => x.AccessTokenExpirySeconds).Returns(3600);
        _passwordHasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("hashed-password");
    }

    private AuthController CriarSut()
    {
        RegisterUser registerUser = new(_userRepository.Object, _passwordHasher.Object, _tokenProvider.Object);
        LoginUser loginUser = new(_userRepository.Object, _passwordHasher.Object, _tokenProvider.Object);
        RefreshToken refreshToken = new(_userRepository.Object, _tokenProvider.Object);
        return new AuthController(registerUser, loginUser, refreshToken);
    }

    private void ConfigurarValidacaoComSucesso<T>(Mock<IValidator<T>> validatorMock)
    {
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private void ConfigurarValidacaoComFalha<T>(Mock<IValidator<T>> validatorMock, string mensagemErro = "Campo obrigatório")
    {
        ValidationResult resultado = new(new[]
        {
            new ValidationFailure("Campo", mensagemErro)
        });
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultado);
    }

    // ── Register ────────────────────────────────────────────────────

    [Fact]
    public async Task Given_RegistroComValidacaoFalha_Should_RetornarBadRequest()
    {
        // Arrange
        AuthController sut = CriarSut();
        ConfigurarValidacaoComFalha(_registerValidator, "Email é obrigatório");
        RegisterRequest request = new() { Email = "", Nome = "Teste", Senha = "123456" };

        // Act
        IActionResult resultado = await sut.Register(request, _registerValidator.Object);

        // Assert
        BadRequestObjectResult badRequest = resultado.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value!.ToString().ShouldContain("Validação falhou");
    }

    [Fact]
    public async Task Given_RegistroComSucesso_Should_Retornar201ComTokens()
    {
        // Arrange
        AuthController sut = CriarSut();
        ConfigurarValidacaoComSucesso(_registerValidator);
        _userRepository.Setup(r => r.GetByEmailAsync("novo@teste.com")).ReturnsAsync((User?)null);
        _userRepository.Setup(r => r.CreateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        RegisterRequest request = new() { Email = "novo@teste.com", Nome = "Novo Usuário", Senha = "senha123" };

        // Act
        IActionResult resultado = await sut.Register(request, _registerValidator.Object);

        // Assert
        ObjectResult objectResult = resultado.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(201);
        AuthResponse authResponse = objectResult.Value.ShouldBeOfType<AuthResponse>();
        authResponse.AccessToken.ShouldBe("access-token");
        authResponse.RefreshToken.ShouldBe("refresh-token");
        authResponse.ExpiresIn.ShouldBe(3600);
    }

    [Fact]
    public async Task Given_RegistroComEmailDuplicado_Should_RetornarConflict()
    {
        // Arrange
        AuthController sut = CriarSut();
        ConfigurarValidacaoComSucesso(_registerValidator);
        User usuarioExistente = User.Create("existente@teste.com", "Existente", "hash");
        _userRepository.Setup(r => r.GetByEmailAsync("existente@teste.com")).ReturnsAsync(usuarioExistente);
        RegisterRequest request = new() { Email = "existente@teste.com", Nome = "Outro", Senha = "senha123" };

        // Act
        IActionResult resultado = await sut.Register(request, _registerValidator.Object);

        // Assert
        ConflictObjectResult conflict = resultado.ShouldBeOfType<ConflictObjectResult>();
        conflict.Value!.ToString().ShouldContain("Email já cadastrado");
    }

    // ── Login ───────────────────────────────────────────────────────

    [Fact]
    public async Task Given_LoginComValidacaoFalha_Should_RetornarBadRequest()
    {
        // Arrange
        AuthController sut = CriarSut();
        ConfigurarValidacaoComFalha(_loginValidator, "Senha é obrigatória");
        LoginRequest request = new() { Email = "user@teste.com", Senha = "" };

        // Act
        IActionResult resultado = await sut.Login(request, _loginValidator.Object);

        // Assert
        BadRequestObjectResult badRequest = resultado.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value!.ToString().ShouldContain("Validação falhou");
    }

    [Fact]
    public async Task Given_LoginComSucesso_Should_RetornarOkComTokens()
    {
        // Arrange
        AuthController sut = CriarSut();
        ConfigurarValidacaoComSucesso(_loginValidator);
        User usuario = User.Create("user@teste.com", "Usuário", "hash-senha");
        _userRepository.Setup(r => r.GetByEmailAsync("user@teste.com")).ReturnsAsync(usuario);
        _passwordHasher.Setup(p => p.Verify("senha123", "hash-senha")).Returns(true);
        LoginRequest request = new() { Email = "user@teste.com", Senha = "senha123" };

        // Act
        IActionResult resultado = await sut.Login(request, _loginValidator.Object);

        // Assert
        OkObjectResult ok = resultado.ShouldBeOfType<OkObjectResult>();
        AuthResponse authResponse = ok.Value.ShouldBeOfType<AuthResponse>();
        authResponse.AccessToken.ShouldBe("access-token");
        authResponse.RefreshToken.ShouldBe("refresh-token");
    }

    [Fact]
    public async Task Given_LoginComCredenciaisInvalidas_Should_RetornarUnauthorized()
    {
        // Arrange
        AuthController sut = CriarSut();
        ConfigurarValidacaoComSucesso(_loginValidator);
        User usuario = User.Create("user@teste.com", "Usuário", "hash-senha");
        _userRepository.Setup(r => r.GetByEmailAsync("user@teste.com")).ReturnsAsync(usuario);
        _passwordHasher.Setup(p => p.Verify("senha-errada", "hash-senha")).Returns(false);
        LoginRequest request = new() { Email = "user@teste.com", Senha = "senha-errada" };

        // Act
        IActionResult resultado = await sut.Login(request, _loginValidator.Object);

        // Assert
        UnauthorizedObjectResult unauthorized = resultado.ShouldBeOfType<UnauthorizedObjectResult>();
        unauthorized.Value!.ToString().ShouldContain("Credenciais inválidas");
    }

    [Fact]
    public async Task Given_LoginComUsuarioInexistente_Should_RetornarUnauthorized()
    {
        // Arrange
        AuthController sut = CriarSut();
        ConfigurarValidacaoComSucesso(_loginValidator);
        _userRepository.Setup(r => r.GetByEmailAsync("inexistente@teste.com")).ReturnsAsync((User?)null);
        LoginRequest request = new() { Email = "inexistente@teste.com", Senha = "senha123" };

        // Act
        IActionResult resultado = await sut.Login(request, _loginValidator.Object);

        // Assert
        resultado.ShouldBeOfType<UnauthorizedObjectResult>();
    }

    // ── Refresh ─────────────────────────────────────────────────────

    [Fact]
    public async Task Given_RefreshComValidacaoFalha_Should_RetornarBadRequest()
    {
        // Arrange
        AuthController sut = CriarSut();
        ConfigurarValidacaoComFalha(_refreshValidator, "Token é obrigatório");
        RefreshRequest request = new() { RefreshToken = "" };

        // Act
        IActionResult resultado = await sut.Refresh(request, _refreshValidator.Object);

        // Assert
        BadRequestObjectResult badRequest = resultado.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value!.ToString().ShouldContain("Validação falhou");
    }

    [Fact]
    public async Task Given_RefreshComSucesso_Should_RetornarOkComNovosTokens()
    {
        // Arrange
        AuthController sut = CriarSut();
        ConfigurarValidacaoComSucesso(_refreshValidator);
        User usuario = User.Create("user@teste.com", "Usuário", "hash");
        usuario.SetId("user-id-123");
        _tokenProvider.Setup(t => t.GetUserIdFromToken("token-valido")).Returns("user-id-123");
        _userRepository.Setup(r => r.GetByIdAsync("user-id-123")).ReturnsAsync(usuario);
        RefreshRequest request = new() { RefreshToken = "token-valido" };

        // Act
        IActionResult resultado = await sut.Refresh(request, _refreshValidator.Object);

        // Assert
        OkObjectResult ok = resultado.ShouldBeOfType<OkObjectResult>();
        AuthResponse authResponse = ok.Value.ShouldBeOfType<AuthResponse>();
        authResponse.AccessToken.ShouldBe("access-token");
        authResponse.RefreshToken.ShouldBe("refresh-token");
    }

    [Fact]
    public async Task Given_RefreshComTokenInvalido_Should_RetornarUnauthorized()
    {
        // Arrange
        AuthController sut = CriarSut();
        ConfigurarValidacaoComSucesso(_refreshValidator);
        _tokenProvider.Setup(t => t.GetUserIdFromToken("token-invalido")).Returns((string?)null);
        RefreshRequest request = new() { RefreshToken = "token-invalido" };

        // Act
        IActionResult resultado = await sut.Refresh(request, _refreshValidator.Object);

        // Assert
        UnauthorizedObjectResult unauthorized = resultado.ShouldBeOfType<UnauthorizedObjectResult>();
        unauthorized.Value!.ToString().ShouldContain("Token inválido");
    }
}
