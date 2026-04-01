using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace MapaTributario.API.Infrastructure.Auth;

public class JwtTokenProvider : ITokenProvider
{
    private readonly IConfiguration _configuration;
    private readonly SymmetricSecurityKey _key;

    public JwtTokenProvider(IConfiguration configuration)
    {
        _configuration = configuration;
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["JWT:Secret"] ?? "default-dev-secret-change-in-production-32chars"));
    }

    public int AccessTokenExpirySeconds =>
        _configuration.GetValue<int>("JWT:ExpiryMinutes", 60) * 60;

    public string GenerateAccessToken(User user)
    {
        int expiryMinutes = _configuration.GetValue<int>("JWT:ExpiryMinutes", 60);
        return GenerateJwt(user, TimeSpan.FromMinutes(expiryMinutes));
    }

    public string GenerateRefreshToken(User user)
    {
        int refreshExpiryDays = _configuration.GetValue<int>("JWT:RefreshExpiryDays", 7);
        return GenerateJwt(user, TimeSpan.FromDays(refreshExpiryDays));
    }

    public string? GetUserIdFromToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _key,
                ValidateIssuer = true,
                ValidIssuer = _configuration["JWT:Issuer"] ?? "MapaTributario",
                ValidateAudience = true,
                ValidAudience = _configuration["JWT:Audience"] ?? "MapaTributario",
                ValidateLifetime = false
            }, out _);

            return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        catch
        {
            return null;
        }
    }

    private string GenerateJwt(User user, TimeSpan expiry)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Nome)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["JWT:Issuer"] ?? "MapaTributario",
            audience: _configuration["JWT:Audience"] ?? "MapaTributario",
            claims: claims,
            expires: DateTime.UtcNow.Add(expiry),
            signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
