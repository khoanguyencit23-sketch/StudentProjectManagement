using Microsoft.EntityFrameworkCore;
using StudentProjectManagement.Domain.Entities;
using StudentProjectManagement.Domain.Enums;

namespace StudentProjectManagement.Infrastructure.Persistence;

public static class DbSeeder
{
    /// <summary>
    /// Tạo danh mục role ở mọi môi trường. Tài khoản mẫu chỉ được tạo trong Development.
    /// </summary>
    public static async Task SeedAsync(
        ApplicationDbContext dbContext,
        bool isDevelopment,
        CancellationToken cancellationToken = default)
    {
        var roleDefinitions = new[]
        {
            new { Name = nameof(UserRole.Admin), Description = "System administrator" },
            new { Name = nameof(UserRole.Lecturer), Description = "Project supervisor / lecturer" },
            new { Name = nameof(UserRole.Student), Description = "Student account" }
        };

        foreach (var definition in roleDefinitions)
        {
            var roleExists = await dbContext.Roles.AnyAsync(
                x => x.Name == definition.Name,
                cancellationToken);

            if (!roleExists)
            {
                dbContext.Roles.Add(new Role
                {
                    Name = definition.Name,
                    Description = definition.Description
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (!isDevelopment)
        {
            return;
        }

        var demoAccounts = new[]
        {
            new DemoAccount("System Admin", "admin@spm.local", "Admin@12345", nameof(UserRole.Admin)),
            new DemoAccount("Lecturer One", "lecturer1@spm.local", "Lecturer@12345", nameof(UserRole.Lecturer)),
            new DemoAccount("Lecturer Two", "lecturer2@spm.local", "Lecturer@12345", nameof(UserRole.Lecturer)),
            new DemoAccount("Student One", "student1@spm.local", "Student@12345", nameof(UserRole.Student)),
            new DemoAccount("Student Two", "student2@spm.local", "Student@12345", nameof(UserRole.Student)),
            new DemoAccount("Student Three", "student3@spm.local", "Student@12345", nameof(UserRole.Student))
        };

        foreach (var account in demoAccounts)
        {
            var normalizedEmail = account.Email.Trim().ToUpperInvariant();
            var exists = await dbContext.Users.AnyAsync(
                x => x.NormalizedEmail == normalizedEmail,
                cancellationToken);

            if (exists)
            {
                continue;
            }

            var role = await dbContext.Roles.SingleAsync(
                x => x.Name == account.Role,
                cancellationToken);

            var user = new User
            {
                FullName = account.FullName,
                Email = account.Email,
                NormalizedEmail = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(account.Password, 12),
                IsActive = true
            };

            user.UserRoles.Add(new UserRoleAssignment
            {
                UserId = user.Id,
                RoleId = role.Id
            });

            dbContext.Users.Add(user);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record DemoAccount(string FullName, string Email, string Password, string Role);
}
