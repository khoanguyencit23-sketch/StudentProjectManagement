namespace StudentProjectManagement.Application.Auth;

public sealed record TokenResult(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
