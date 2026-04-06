#pragma warning disable CS8604 // ToString() em objetos anônimos retorna string? mas nunca é null nos testes
using System.Security.Claims;
using FluentValidation;
using FluentValidation.Results;
using MapaTributario.API.Application.Perfil.Contracts;
using MapaTributario.API.Controllers;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class PerfilControllerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ITokenProvider> _tokenProvider = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IValidator<AtualizarPerfilRequest>> _atualizarValidator = new();

    private const string UsuarioId = "user-id-123";
    private const string UsuarioEmail = "usuario@teste.com";
    private const string UsuarioNome = "João Silva";
    private const string SenhaHash = "hash-senha-atual";

    public PerfilControllerTests()
    {
        _tokenProvider.Setup(x => x.GenerateAccessToken(It.IsAny<User>())).Returns("novo-access-token");
        _passwordHasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("novo-hash");
    }

    private PerfilController CriarSut(string? userId = UsuarioId)
    {
        var controller = new PerfilController(
            _userRepository.Object,
            _tokenProvider.Object,
            _passwordHasher.Object);

        var claims = new List<Claim>();
        if (userId is not null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        return controller;
    }

    private User CriarUsuario()
    {
        var usuario = User.Create(UsuarioEmail, UsuarioNome, SenhaHash);
        usuario.SetId(UsuarioId);
        return usuario;
    }

    private void ConfigurarValidacaoComSucesso()
    {
        _atualizarValidator.Setup(v => v.ValidateAsync(It.IsAny<AtualizarPerfilRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private void ConfigurarValidacaoComFalha(string mensagemErro = "Campo obrigatório")
    {
        var resultado = new ValidationResult(new[]
        {
            new ValidationFailure("Campo", mensagemErro)
        });
        _atualizarValidator.Setup(v => v.ValidateAsync(It.IsAny<AtualizarPerfilRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultado);
    }

    // ── ObterPerfil (GET) ───────────────────────────────────────────

    [Fact]
    public async Task Given_UsuarioAutenticadoComIdValido_Should_RetornarOkComPerfilResponse()
    {
        // Arrange
        var sut = CriarSut();
        var usuario = CriarUsuario();
        _userRepository.Setup(r => r.GetByIdAsync(UsuarioId)).ReturnsAsync(usuario);

        // Act
        var resultado = await sut.ObterPerfil();

        // Assert
        OkObjectResult ok = resultado.ShouldBeOfType<OkObjectResult>();
        PerfilResponse perfil = ok.Value.ShouldBeOfType<PerfilResponse>();
        perfil.Id.ShouldBe(UsuarioId);
        perfil.Nome.ShouldBe(UsuarioNome);
        perfil.Email.ShouldBe(UsuarioEmail);
    }

    [Fact]
    public async Task Given_UsuarioNaoEncontrado_Should_RetornarNotFound()
    {
        // Arrange
        var sut = CriarSut();
        _userRepository.Setup(r => r.GetByIdAsync(UsuarioId)).ReturnsAsync((User?)null);

        // Act
        var resultado = await sut.ObterPerfil();

        // Assert
        NotFoundObjectResult notFound = resultado.ShouldBeOfType<NotFoundObjectResult>();
        notFound.Value!.ToString().ShouldContain("Usuário não encontrado");
    }

    [Fact]
    public async Task Given_UsuarioSemClaimDeId_ObterPerfil_Should_RetornarUnauthorized()
    {
        // Arrange
        var sut = CriarSut(null);

        // Act
        var resultado = await sut.ObterPerfil();

        // Assert
        UnauthorizedObjectResult unauthorized = resultado.ShouldBeOfType<UnauthorizedObjectResult>();
        unauthorized.Value!.ToString().ShouldContain("Usuário não identificado");
    }

    // ── AtualizarPerfil (PUT) ───────────────────────────────────────

    [Fact]
    public async Task Given_AtualizarSomenteNome_Should_RetornarOkComNovoToken()
    {
        // Arrange
        var sut = CriarSut();
        var usuario = CriarUsuario();
        ConfigurarValidacaoComSucesso();
        _userRepository.Setup(r => r.GetByIdAsync(UsuarioId)).ReturnsAsync(usuario);
        _userRepository.Setup(r => r.AtualizarAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        var request = new AtualizarPerfilRequest { Nome = "Novo Nome" };

        // Act
        var resultado = await sut.AtualizarPerfil(request, _atualizarValidator.Object);

        // Assert
        OkObjectResult ok = resultado.ShouldBeOfType<OkObjectResult>();
        AtualizarPerfilResponse response = ok.Value.ShouldBeOfType<AtualizarPerfilResponse>();
        response.Nome.ShouldBe("Novo Nome");
        response.Email.ShouldBe(UsuarioEmail);
        response.AccessToken.ShouldBe("novo-access-token");
    }

    [Fact]
    public async Task Given_AtualizarNomeESenhaComSenhaAtualCorreta_Should_RetornarOk()
    {
        // Arrange
        var sut = CriarSut();
        var usuario = CriarUsuario();
        ConfigurarValidacaoComSucesso();
        _userRepository.Setup(r => r.GetByIdAsync(UsuarioId)).ReturnsAsync(usuario);
        _userRepository.Setup(r => r.AtualizarAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _passwordHasher.Setup(p => p.Verify("senhaAtual123", SenhaHash)).Returns(true);
        var request = new AtualizarPerfilRequest
        {
            Nome = "Novo Nome",
            SenhaAtual = "senhaAtual123",
            NovaSenha = "novaSenha456"
        };

        // Act
        var resultado = await sut.AtualizarPerfil(request, _atualizarValidator.Object);

        // Assert
        OkObjectResult ok = resultado.ShouldBeOfType<OkObjectResult>();
        AtualizarPerfilResponse response = ok.Value.ShouldBeOfType<AtualizarPerfilResponse>();
        response.AccessToken.ShouldBe("novo-access-token");
        _passwordHasher.Verify(p => p.Hash("novaSenha456"), Times.Once);
    }

    [Fact]
    public async Task Given_SenhaAtualIncorreta_Should_RetornarBadRequest()
    {
        // Arrange
        var sut = CriarSut();
        var usuario = CriarUsuario();
        ConfigurarValidacaoComSucesso();
        _userRepository.Setup(r => r.GetByIdAsync(UsuarioId)).ReturnsAsync(usuario);
        _passwordHasher.Setup(p => p.Verify("senhaErrada", SenhaHash)).Returns(false);
        var request = new AtualizarPerfilRequest
        {
            Nome = "Novo Nome",
            SenhaAtual = "senhaErrada",
            NovaSenha = "novaSenha456"
        };

        // Act
        var resultado = await sut.AtualizarPerfil(request, _atualizarValidator.Object);

        // Assert
        BadRequestObjectResult badRequest = resultado.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value!.ToString().ShouldContain("Senha atual incorreta");
    }

    [Fact]
    public async Task Given_ValidacaoFalhaComNomeVazio_Should_RetornarBadRequestComDetalhes()
    {
        // Arrange
        var sut = CriarSut();
        ConfigurarValidacaoComFalha("Nome é obrigatório");
        var request = new AtualizarPerfilRequest { Nome = "" };

        // Act
        var resultado = await sut.AtualizarPerfil(request, _atualizarValidator.Object);

        // Assert
        BadRequestObjectResult badRequest = resultado.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value!.ToString().ShouldContain("Validação falhou");
    }

    [Fact]
    public async Task Given_UsuarioSemClaimDeId_AtualizarPerfil_Should_RetornarUnauthorized()
    {
        // Arrange
        var sut = CriarSut(null);
        ConfigurarValidacaoComSucesso();
        var request = new AtualizarPerfilRequest { Nome = "Novo Nome" };

        // Act
        var resultado = await sut.AtualizarPerfil(request, _atualizarValidator.Object);

        // Assert
        UnauthorizedObjectResult unauthorized = resultado.ShouldBeOfType<UnauthorizedObjectResult>();
        unauthorized.Value!.ToString().ShouldContain("Usuário não identificado");
    }

    [Fact]
    public async Task Given_UsuarioNaoEncontrado_AtualizarPerfil_Should_RetornarNotFound()
    {
        // Arrange
        var sut = CriarSut();
        ConfigurarValidacaoComSucesso();
        _userRepository.Setup(r => r.GetByIdAsync(UsuarioId)).ReturnsAsync((User?)null);
        var request = new AtualizarPerfilRequest { Nome = "Novo Nome" };

        // Act
        var resultado = await sut.AtualizarPerfil(request, _atualizarValidator.Object);

        // Assert
        NotFoundObjectResult notFound = resultado.ShouldBeOfType<NotFoundObjectResult>();
        notFound.Value!.ToString().ShouldContain("Usuário não encontrado");
    }
}
