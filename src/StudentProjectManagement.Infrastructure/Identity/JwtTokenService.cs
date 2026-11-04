using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StudentProjectManagement.Application.Auth;
using StudentProjectManagement.Application.Auth.Interfaces;
using StudentProjectManagement.Domain.Entities;
using StudentProjectManagement.Infrastructure.Options;

namespace StudentProjectManagement.Infrastructure.Identity;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public TokenResult GenerateTokens(User user, string roleName)
    {
        var now = DateTime.UtcNow;
        var accessExpiresAt = new DateTimeOffset(now.AddMinutes(_options.AccessTokenMinutes), TimeSpan.Zero);
        var refreshExpiresAt = DateTimeOffset.UtcNow.AddDays(_options.RefreshTokenDays);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, roleName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: accessExpiresAt.UtcDateTime,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = GenerateSecureRefreshToken();

        return new TokenResult(accessToken, accessExpiresAt, refreshToken, refreshExpiresAt);
    }

    /// <summary>Hash ổn định để có thể tìm refresh token trong DB mà không lưu token gốc.</summary>
    public string HashRefreshToken(string refreshToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(hash);
    }

    private static string GenerateSecureRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
