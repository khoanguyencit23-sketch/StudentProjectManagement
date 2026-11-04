using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using StudentProjectManagement.Application.Common.Interfaces;
using StudentProjectManagement.Domain.Common;
using StudentProjectManagement.Domain.Entities;

namespace StudentProjectManagement.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    /// <summary>Bảng nối User-Role; tên DbSet theo yêu cầu schema của dự án.</summary>
    public DbSet<UserRoleAssignment> UserRoles => Set<UserRoleAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.NormalizedEmail).HasMaxLength(256).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(x => x.RefreshTokenHash).HasMaxLength(64);
            entity.HasIndex(x => x.NormalizedEmail).IsUnique();
            entity.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(200);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<UserRoleAssignment>(entity =>
        {
            entity.ToTable("UserRoles");
            entity.HasKey(x => new { x.UserId, x.RoleId });
            entity.Property(x => x.AssignedAt).IsRequired();
            entity.HasOne(x => x.User)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Role)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override int SaveChanges()
    {
        ApplyAuditInformation();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Tự động ghi audit fields và chuyển thao tác Remove thành xóa mềm.
    /// </summary>
    private void ApplyAuditInformation()
    {
        var now = DateTimeOffset.UtcNow;
        var currentUserId = _currentUserService.UserId;

        foreach (EntityEntry<BaseEntity> entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
            {
                continue;
            }

            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
            }

            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = now;
                }

                entry.Entity.CreatedBy ??= currentUserId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = currentUserId;
                entry.Property(x => x.CreatedAt).IsModified = false;
                entry.Property(x => x.CreatedBy).IsModified = false;
            }
        }
    }
}
