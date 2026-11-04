using StudentProjectManagement.Domain.Common;

namespace StudentProjectManagement.Domain.Entities;

public sealed class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>Địa chỉ email đã chuẩn hóa để kiểm tra trùng không phân biệt hoa/thường.</summary>
    public string NormalizedEmail { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    /// <summary>Chỉ lưu SHA-256 hash của refresh token, không lưu token thô.</summary>
    public string? RefreshTokenHash { get; set; }

    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }

    public ICollection<UserRoleAssignment> UserRoles { get; set; } = new List<UserRoleAssignment>();
}
