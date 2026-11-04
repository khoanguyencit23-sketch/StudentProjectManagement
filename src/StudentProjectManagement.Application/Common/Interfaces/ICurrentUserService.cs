namespace StudentProjectManagement.Application.Common.Interfaces;

/// <summary>Đọc danh tính đã được xác thực từ ClaimsPrincipal của request hiện tại.</summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }

    string? Email { get; }

    string? Role { get; }

    bool IsAuthenticated { get; }
}
