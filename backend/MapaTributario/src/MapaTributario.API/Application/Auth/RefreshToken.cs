using FluentResults;
using MapaTributario.API.Application.Auth.Contracts;
using MapaTributario.API.Domain.Interfaces;

namespace MapaTributario.API.Application.Auth;

public class RefreshToken
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenProvider _tokenProvider;

    public RefreshToken(IUserRepository userRepository, ITokenProvider tokenProvider)
    {
        _userRepository = userRepository;
        _tokenProvider = tokenProvider;
    }

    public async Task<Result<AuthResponse>> ExecuteAsync(RefreshRequest request)
    {
        string? userId = _tokenProvider.GetUserIdFromToken(request.RefreshToken);
        if (userId is null)
        {
            return Result.Fail<AuthResponse>("Token inválido");
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null || !user.Ativo)
        {
            return Result.Fail<AuthResponse>("Usuário não encontrado ou inativo");
        }

        return Result.Ok(new AuthResponse
        {
            AccessToken = _tokenProvider.GenerateAccessToken(user),
            RefreshToken = _tokenProvider.GenerateRefreshToken(user),
            ExpiresIn = _tokenProvider.AccessTokenExpirySeconds
        });
    }
}
