namespace StudentProjectManagement.Domain.Enums;

/// <summary>
/// Trạng thái vòng đời cơ bản của một đồ án sinh viên.
/// Có thể mở rộng khi thống nhất quy trình nghiệp vụ với giảng viên.
/// </summary>
public enum ProjectStatus
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    InProgress = 4,
    Completed = 5,
    Rejected = 6,
    Cancelled = 7
}
