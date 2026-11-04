namespace StudentProjectManagement.Domain.Enums;

/// <summary>
/// Các vai trò nghiệp vụ được hỗ trợ trong hệ thống.
/// Role vẫn được lưu trong bảng Roles để hỗ trợ quan hệ dữ liệu.
/// </summary>
public enum UserRole
{
    Admin = 1,
    Lecturer = 2,
    Student = 3
}
