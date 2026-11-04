using System.ComponentModel.DataAnnotations;

namespace StudentProjectManagement.Application.Auth.DTOs;

public sealed class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}
