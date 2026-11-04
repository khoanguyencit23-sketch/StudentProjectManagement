namespace StudentProjectManagement.Domain.Common;

/// <summary>
/// Các thuộc tính dùng chung cho entity có vòng đời độc lập.
/// ID được tạo ở phía ứng dụng để entity có định danh ngay khi khởi tạo.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// Đánh dấu xóa mềm. DbContext tự động lọc các bản ghi đã xóa.
    /// </summary>
    public bool IsDeleted { get; set; }
}
