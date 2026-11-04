using Microsoft.EntityFrameworkCore;
using StudentProjectManagement.Infrastructure.Persistence;

#nullable disable

namespace StudentProjectManagement.Infrastructure.Persistence.Migrations;

public sealed partial class InitialCreate
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.32")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        modelBuilder.Entity("StudentProjectManagement.Domain.Entities.Role", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAt").HasColumnType("datetimeoffset");
            b.Property<Guid?>("CreatedBy").HasColumnType("uniqueidentifier");
            b.Property<string>("Description").HasMaxLength(200).HasColumnType("nvarchar(200)");
            b.Property<bool>("IsDeleted").HasColumnType("bit");
            b.Property<string>("Name").IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<DateTimeOffset?>("UpdatedAt").HasColumnType("datetimeoffset");
            b.Property<Guid?>("UpdatedBy").HasColumnType("uniqueidentifier");
            b.HasKey("Id");
            b.HasIndex("Name").IsUnique();
            b.ToTable("Roles");
        });

        modelBuilder.Entity("StudentProjectManagement.Domain.Entities.User", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAt").HasColumnType("datetimeoffset");
            b.Property<Guid?>("CreatedBy").HasColumnType("uniqueidentifier");
            b.Property<string>("Email").IsRequired().HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<string>("FullName").IsRequired().HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<bool>("IsActive").HasColumnType("bit");
            b.Property<bool>("IsDeleted").HasColumnType("bit");
            b.Property<string>("NormalizedEmail").IsRequired().HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<string>("PasswordHash").IsRequired().HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<string>("RefreshTokenHash").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("RefreshTokenExpiresAt").HasColumnType("datetimeoffset");
            b.Property<DateTimeOffset?>("UpdatedAt").HasColumnType("datetimeoffset");
            b.Property<Guid?>("UpdatedBy").HasColumnType("uniqueidentifier");
            b.HasKey("Id");
            b.HasIndex("NormalizedEmail").IsUnique();
            b.ToTable("Users");
        });

        modelBuilder.Entity("StudentProjectManagement.Domain.Entities.UserRoleAssignment", b =>
        {
            b.Property<Guid>("UserId").HasColumnType("uniqueidentifier");
            b.Property<Guid>("RoleId").HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("AssignedAt").HasColumnType("datetimeoffset");
            b.HasKey("UserId", "RoleId");
            b.HasIndex("RoleId");
            b.ToTable("UserRoles");
        });

        modelBuilder.Entity("StudentProjectManagement.Domain.Entities.UserRoleAssignment", b =>
        {
            b.HasOne("StudentProjectManagement.Domain.Entities.Role", "Role")
                .WithMany("UserRoles")
                .HasForeignKey("RoleId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
            b.HasOne("StudentProjectManagement.Domain.Entities.User", "User")
                .WithMany("UserRoles")
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            b.Navigation("Role");
            b.Navigation("User");
        });

        modelBuilder.Entity("StudentProjectManagement.Domain.Entities.Role", b =>
        {
            b.Navigation("UserRoles");
        });

        modelBuilder.Entity("StudentProjectManagement.Domain.Entities.User", b =>
        {
            b.Navigation("UserRoles");
        });
#pragma warning restore 612, 618
    }
}
