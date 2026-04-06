using System.Security.Claims;
using FluentValidation;
using MapaTributario.API.Application.Perfil.Contracts;
using MapaTributario.API.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapaTributario.API.Controllers;

[ApiController]
[Route("api/v1/perfil")]
[Authorize]
public class PerfilController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenProvider _tokenProvider;
    private readonly IPasswordHasher _passwordHasher;

    public PerfilController(
        IUserRepository userRepository,
        ITokenProvider tokenProvider,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _tokenProvider = tokenProvider;
        _passwordHasher = passwordHasher;
    }

    [HttpGet]
    public async Task<IActionResult> ObterPerfil()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
            return Unauthorized(new { erro = "Usuário não identificado" });

        var usuario = await _userRepository.GetByIdAsync(userId);
        if (usuario is null)
            return NotFound(new { erro = "Usuário não encontrado" });

        return Ok(new PerfilResponse
        {
            Id = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email
        });
    }

    [HttpPut]
    public async Task<IActionResult> AtualizarPerfil(
        [FromBody] AtualizarPerfilRequest request,
        [FromServices] IValidator<AtualizarPerfilRequest> validator)
    {
        var validationError = await ValidarRequestAsync(request, validator);
        if (validationError is not null) return validationError;

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
            return Unauthorized(new { erro = "Usuário não identificado" });

        var usuario = await _userRepository.GetByIdAsync(userId);
        if (usuario is null)
            return NotFound(new { erro = "Usuário não encontrado" });

        usuario.AtualizarNome(request.Nome);

        if (!string.IsNullOrEmpty(request.NovaSenha))
        {
            if (!_passwordHasher.Verify(request.SenhaAtual!, usuario.PasswordHash))
                return BadRequest(new { erro = "Senha atual incorreta" });

            var novoHash = _passwordHasher.Hash(request.NovaSenha);
            usuario.AtualizarSenha(novoHash);
        }

        await _userRepository.AtualizarAsync(usuario);

        var novoToken = _tokenProvider.GenerateAccessToken(usuario);

        return Ok(new AtualizarPerfilResponse
        {
            Id = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email,
            AccessToken = novoToken
        });
    }

    private static async Task<BadRequestObjectResult?> ValidarRequestAsync<T>(T request, IValidator<T> validator)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return new BadRequestObjectResult(new { erro = "Validação falhou", detalhes = validation.Errors.Select(e => e.ErrorMessage).ToArray() });
        }
        return null;
    }
}
