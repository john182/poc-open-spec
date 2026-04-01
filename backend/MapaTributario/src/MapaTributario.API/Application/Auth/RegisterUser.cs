using FluentResults;
using MapaTributario.API.Application.Auth.Contracts;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;

namespace MapaTributario.API.Application.Auth;

public class RegisterUser
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenProvider _tokenProvider;

    public RegisterUser(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenProvider tokenProvider)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenProvider = tokenProvider;
    }

    public async Task<Result<AuthResponse>> ExecuteAsync(RegisterRequest request)
    {
        var existing = await _userRepository.GetByEmailAsync(request.Email);
        if (existing is not null)
        {
            return Result.Fail<AuthResponse>("Email já cadastrado");
        }

        string hash = _passwordHasher.Hash(request.Senha);
        var user = User.Create(request.Email, request.Nome, hash);

        await _userRepository.CreateAsync(user);

        return Result.Ok(new AuthResponse
        {
            AccessToken = _tokenProvider.GenerateAccessToken(user),
            RefreshToken = _tokenProvider.GenerateRefreshToken(user),
            ExpiresIn = _tokenProvider.AccessTokenExpirySeconds
        });
    }
}
