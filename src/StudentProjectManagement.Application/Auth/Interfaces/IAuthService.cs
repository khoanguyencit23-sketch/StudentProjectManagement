using StudentProjectManagement.Application.Auth.DTOs;

namespace StudentProjectManagement.Application.Auth.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task RevokeRefreshTokenAsync(Guid userId, CancellationToken cancellationToken = default);
}
