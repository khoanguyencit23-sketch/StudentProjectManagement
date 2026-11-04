using StudentProjectManagement.Domain.Entities;

namespace StudentProjectManagement.Application.Auth.Interfaces;

public interface IJwtTokenService
{
    TokenResult GenerateTokens(User user, string roleName);

    string HashRefreshToken(string refreshToken);
}
