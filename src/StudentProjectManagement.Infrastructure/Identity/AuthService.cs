using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using StudentProjectManagement.Application.Auth;
using StudentProjectManagement.Application.Auth.DTOs;
using StudentProjectManagement.Application.Auth.Interfaces;
using StudentProjectManagement.Domain.Entities;
using StudentProjectManagement.Domain.Enums;
using StudentProjectManagement.Infrastructure.Persistence;

namespace StudentProjectManagement.Infrastructure.Identity;

/// <summary>Use-case xác thực. API Controller chỉ gọi service, không truy cập DbContext.</summary>
public sealed class AuthService : IAuthService
{
    private const int BCryptWorkFactor = 12;

    private readonly ApplicationDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(ApplicationDbContext dbContext, IJwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var fullName = request.FullName.Trim();
        var email = request.Email.Trim();
        var normalizedEmail = NormalizeEmail(email);
        ValidateEmailAndPassword(email, request.Password);

        var exists = await _dbContext.Users.AnyAsync(
            x => x.NormalizedEmail == normalizedEmail,
            cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        // Đăng ký công khai luôn là Student. Không nhận Role từ request để tránh tự nâng quyền.
        var studentRole = await _dbContext.Roles.SingleOrDefaultAsync(
            x => x.Name == nameof(UserRole.Student),
            cancellationToken);

        if (studentRole is null)
        {
            throw new InvalidOperationException("Student role is missing. Run the database seeder first.");
        }

        var user = new User
        {
            FullName = fullName,
            Email = email,
            NormalizedEmail = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, BCryptWorkFactor),
            IsActive = true
        };

        var tokens = _jwtTokenService.GenerateTokens(user, studentRole.Name);
        user.RefreshTokenHash = _jwtTokenService.HashRefreshToken(tokens.RefreshToken);
        user.RefreshTokenExpiresAt = tokens.RefreshTokenExpiresAt;

        user.UserRoles.Add(new UserRoleAssignment
        {
            UserId = user.Id,
            RoleId = studentRole.Id
        });

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToAuthResponse(user, studentRole.Name, tokens);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var user = await LoadUserByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            // Không tiết lộ email có tồn tại hay không.
            throw new UnauthorizedAccessException("Email or password is incorrect.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("This account is inactive.");
        }

        var roleName = GetPrimaryRoleName(user);
        var tokens = _jwtTokenService.GenerateTokens(user, roleName);
        user.RefreshTokenHash = _jwtTokenService.HashRefreshToken(tokens.RefreshToken);
        user.RefreshTokenExpiresAt = tokens.RefreshTokenExpiresAt;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToAuthResponse(user, roleName, tokens);
    }

    public async Task<AuthResponse> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ArgumentException("Refresh token is required.", nameof(refreshToken));
        }

        var tokenHash = _jwtTokenService.HashRefreshToken(refreshToken);
        var now = DateTimeOffset.UtcNow;
        var user = await _dbContext.Users
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(
                x => x.RefreshTokenHash == tokenHash && x.RefreshTokenExpiresAt > now,
                cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Refresh token is invalid or expired.");
        }

        var roleName = GetPrimaryRoleName(user);
        var tokens = _jwtTokenService.GenerateTokens(user, roleName);

        // Rotation: token cũ bị thay thế ngay sau khi phát hành token mới.
        user.RefreshTokenHash = _jwtTokenService.HashRefreshToken(tokens.RefreshToken);
        user.RefreshTokenExpiresAt = tokens.RefreshTokenExpiresAt;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToAuthResponse(user, roleName, tokens);
    }

    public async Task RevokeRefreshTokenAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(
            x => x.Id == userId,
            cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException("User was not found.");
        }

        user.RefreshTokenHash = null;
        user.RefreshTokenExpiresAt = null;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private Task<User?> LoadUserByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    private static string GetPrimaryRoleName(User user)
    {
        var roleName = user.UserRoles
            .Select(x => x.Role.Name)
            .OrderBy(x => x, StringComparer.Ordinal)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new InvalidOperationException("User has no assigned role.");
        }

        return roleName;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    private static void ValidateEmailAndPassword(string email, string password)
    {
        if (!new EmailAddressAttribute().IsValid(email))
        {
            throw new ArgumentException("A valid email address is required.", nameof(email));
        }

        if (password.Length < 8 || password.Length > 128 ||
            !password.Any(char.IsUpper) ||
            !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit))
        {
            throw new ArgumentException(
                "Password must be 8-128 characters and include uppercase, lowercase, and a number.",
                nameof(password));
        }
    }

    private static AuthResponse ToAuthResponse(User user, string roleName, TokenResult tokens)
    {
        return new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = roleName,
            AccessToken = tokens.AccessToken,
            AccessTokenExpiresAt = tokens.AccessTokenExpiresAt,
            RefreshToken = tokens.RefreshToken,
            RefreshTokenExpiresAt = tokens.RefreshTokenExpiresAt
        };
    }
}
