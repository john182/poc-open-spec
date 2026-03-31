using FluentResults;
using FluentValidation;
using MapaTributario.API.Application.Auth;
using MapaTributario.API.Application.Auth.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace MapaTributario.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly RegisterUser _registerUser;
    private readonly LoginUser _loginUser;
    private readonly RefreshToken _refreshToken;

    public AuthController(RegisterUser registerUser, LoginUser loginUser, RefreshToken refreshToken)
    {
        _registerUser = registerUser;
        _loginUser = loginUser;
        _refreshToken = refreshToken;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        [FromServices] IValidator<RegisterRequest> validator)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(new { erro = "Validação falhou", detalhes = validation.Errors.Select(e => e.ErrorMessage).ToArray() });
        }

        var result = await _registerUser.ExecuteAsync(request);
        return result.IsSuccess
            ? StatusCode(201, result.Value)
            : Conflict(new { erro = result.Errors.First().Message });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] IValidator<LoginRequest> validator)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(new { erro = "Validação falhou", detalhes = validation.Errors.Select(e => e.ErrorMessage).ToArray() });
        }

        var result = await _loginUser.ExecuteAsync(request);
        return result.IsSuccess
            ? Ok(result.Value)
            : Unauthorized(new { erro = result.Errors.First().Message });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequest request,
        [FromServices] IValidator<RefreshRequest> validator)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(new { erro = "Validação falhou", detalhes = validation.Errors.Select(e => e.ErrorMessage).ToArray() });
        }

        var result = await _refreshToken.ExecuteAsync(request);
        return result.IsSuccess
            ? Ok(result.Value)
            : Unauthorized(new { erro = result.Errors.First().Message });
    }
}
