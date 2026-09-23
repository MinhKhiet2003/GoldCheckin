using Base.Authorization;
using Base.Entities;
using Microsoft.EntityFrameworkCore;

namespace Base.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts
    {
        get
        {
            return Set<Account>();
        }
    }

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<UploadedFile> UploadedFiles => Set<UploadedFile>();

    public DbSet<SystemLog> SystemLogs => Set<SystemLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var account = modelBuilder.Entity<Account>();

        account.ToTable("User", table =>
        {
            table.HasCheckConstraint("CK_User_Role", "\"Role\" IN ('admin', 'user')");
        });
        account.Property(item => item.Role).HasMaxLength(20).HasDefaultValue(AppRoles.User).IsRequired();
        account.HasKey(item => item.Id);
        account.Property(item => item.Username).HasMaxLength(50).IsRequired();
        account.Property(item => item.Email).HasMaxLength(254).IsRequired();
        account.HasIndex(item => item.Username).IsUnique();
        account.HasIndex(item => item.Email).IsUnique();
        account.Property(item => item.PasswordHash).HasMaxLength(512).IsRequired();
        account.Property(item => item.FullName).HasMaxLength(150).IsRequired();
        account.Property(item => item.PhoneNumber).HasMaxLength(20);
        account.Property(item => item.AvatarUrl).HasMaxLength(2048);
        account.Property(item => item.Gender).HasMaxLength(30);
        account.Property(item => item.Address).HasMaxLength(500);

        var permission = modelBuilder.Entity<Permission>();
        permission.ToTable("Permission");
        permission.HasKey(item => item.Code);
        permission.Property(item => item.Code).HasMaxLength(100);
        permission.Property(item => item.Name).HasMaxLength(200).IsRequired();
        permission.HasData(
            new Permission { Code = AppPermissions.ReadOwnProfile, Name = "Xem hồ sơ của mình" },
            new Permission { Code = AppPermissions.UpdateOwnProfile, Name = "Cập nhật hồ sơ của mình" },
            new Permission { Code = AppPermissions.ListUsers, Name = "Xem danh sách tài khoản" },
            new Permission { Code = AppPermissions.ReadUser, Name = "Xem chi tiết tài khoản" },
            new Permission { Code = AppPermissions.UpdateUser, Name = "Cập nhật tài khoản" },
            new Permission { Code = AppPermissions.DeleteUser, Name = "Xóa tài khoản" },
            new Permission { Code = AppPermissions.UploadFiles, Name = "Tải file lên" },
            new Permission { Code = AppPermissions.ReadSystemLogs, Name = "Xem nhật ký hệ thống" });

        var rolePermission = modelBuilder.Entity<RolePermission>();
        rolePermission.ToTable("RolePermission", table =>
        {
            table.HasCheckConstraint("CK_RolePermission_Role", "\"Role\" IN ('admin', 'user')");
        });
        rolePermission.HasKey(item => new { item.Role, item.PermissionCode });
        rolePermission.Property(item => item.Role).HasMaxLength(20);
        rolePermission.Property(item => item.PermissionCode).HasMaxLength(100);
        rolePermission.HasOne(item => item.Permission)
            .WithMany()
            .HasForeignKey(item => item.PermissionCode)
            .OnDelete(DeleteBehavior.Cascade);

        var grants = AppPermissions.All.Select(code => new RolePermission
        {
            Role = AppRoles.Admin,
            PermissionCode = code
        }).ToList();
        grants.Add(new RolePermission
        {
            Role = AppRoles.User,
            PermissionCode = AppPermissions.ReadOwnProfile
        });
        grants.Add(new RolePermission
        {
            Role = AppRoles.User,
            PermissionCode = AppPermissions.UpdateOwnProfile
        });
        grants.Add(new RolePermission
        {
            Role = AppRoles.User,
            PermissionCode = AppPermissions.UploadFiles
        });
        rolePermission.HasData(grants);

        var uploadedFile = modelBuilder.Entity<UploadedFile>();
        uploadedFile.ToTable("UploadedFile");
        uploadedFile.HasKey(item => item.Id);
        uploadedFile.Property(item => item.OriginalName).HasMaxLength(255).IsRequired();
        uploadedFile.Property(item => item.StoredName).HasMaxLength(64).IsRequired();
        uploadedFile.HasIndex(item => item.StoredName).IsUnique();
        uploadedFile.Property(item => item.Url).HasMaxLength(256).IsRequired();
        uploadedFile.Property(item => item.ContentType).HasMaxLength(100).IsRequired();
        uploadedFile.HasIndex(item => item.UploadedBy);

        var systemLog = modelBuilder.Entity<SystemLog>();
        systemLog.ToTable("SystemLog");
        systemLog.HasKey(item => item.Id);
        systemLog.Property(item => item.Action).HasMaxLength(30);
        systemLog.Property(item => item.EntityType).HasMaxLength(100);
        systemLog.Property(item => item.ResponseData).HasColumnType("jsonb");
        systemLog.Property(item => item.BeforeData).HasColumnType("jsonb");
        systemLog.Property(item => item.AfterData).HasColumnType("jsonb");
        systemLog.Property(item => item.Method).HasMaxLength(16).IsRequired();
        systemLog.Property(item => item.Path).HasMaxLength(2048).IsRequired();
        systemLog.Property(item => item.Message).HasMaxLength(500).IsRequired();
        systemLog.Property(item => item.IpAddress).HasMaxLength(64);
        systemLog.Property(item => item.TraceId).HasMaxLength(128).IsRequired();
        systemLog.HasIndex(item => item.CreatedAt);
        systemLog.HasIndex(item => new { item.UserId, item.CreatedAt });
        DefaultAccountSeed.Configure(modelBuilder);
    }
}