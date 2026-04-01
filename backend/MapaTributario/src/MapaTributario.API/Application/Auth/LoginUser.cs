using FluentResults;
using MapaTributario.API.Application.Auth.Contracts;
using MapaTributario.API.Domain.Interfaces;

namespace MapaTributario.API.Application.Auth;

public class LoginUser
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenProvider _tokenProvider;

    public LoginUser(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenProvider tokenProvider)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenProvider = tokenProvider;
    }

    public async Task<Result<AuthResponse>> ExecuteAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user is null || !_passwordHasher.Verify(request.Senha, user.PasswordHash))
        {
            return Result.Fail<AuthResponse>("Credenciais inválidas");
        }

        if (!user.Ativo)
        {
            return Result.Fail<AuthResponse>("Conta inativa");
        }

        return Result.Ok(new AuthResponse
        {
            AccessToken = _tokenProvider.GenerateAccessToken(user),
            RefreshToken = _tokenProvider.GenerateRefreshToken(user),
            ExpiresIn = _tokenProvider.AccessTokenExpirySeconds
        });
    }
}
