namespace StudentProjectManagement.Domain.Entities;

/// <summary>
/// Bảng liên kết nhiều-nhiều User và Role. Khóa chính ghép tránh gán trùng role.
/// Tên class khác enum UserRole để tránh xung đột tên trong C#.
/// </summary>
public sealed class UserRoleAssignment
{
    public Guid UserId { get; set; }

    public Guid RoleId { get; set; }

    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;

    public User User { get; set; } = null!;

    public Role Role { get; set; } = null!;
}
