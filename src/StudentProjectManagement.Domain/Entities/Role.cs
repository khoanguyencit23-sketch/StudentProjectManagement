using StudentProjectManagement.Domain.Common;

namespace StudentProjectManagement.Domain.Entities;

public sealed class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<UserRoleAssignment> UserRoles { get; set; } = new List<UserRoleAssignment>();
}
