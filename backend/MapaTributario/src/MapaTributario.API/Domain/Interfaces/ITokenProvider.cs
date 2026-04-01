using MapaTributario.API.Domain.Entities;

namespace MapaTributario.API.Domain.Interfaces;

public interface ITokenProvider
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken(User user);
    string? GetUserIdFromToken(string token);
    int AccessTokenExpirySeconds { get; }
}
