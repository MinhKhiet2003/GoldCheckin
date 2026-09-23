using Base.Authorization;
using Base.Entities;
using Microsoft.EntityFrameworkCore;

namespace Base.Data;

public static class DefaultAccountSeed
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        // Hash cố định của tài khoản khởi tạo admin/admin, không băm lại mỗi lần tạo model.
        var createdAt = new DateTime(2026, 9, 23, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Account>().HasData(new Account
        {
            Id = new Guid("8f5da64b-8e27-4ba4-a8e0-11d088da7931"),
            Username = "admin",
            PasswordHash = "AQAAAAIAAYagAAAAEMGbm6CRE8hR33ytf+bnxKnv92rxeYIkloGWwVTkLYNvsqcxBEZQJWTql0+Tjp2JbQ==",
            FullName = "Quản trị viên hệ thống",
            Email = "admin@example.com",
            PhoneNumber = null,
            AvatarUrl = null,
            DateOfBirth = null,
            Gender = "PreferNotToSay",
            Address = "Việt Nam",
            Role = AppRoles.Admin,
            IsActive = true,
            EmailVerified = false,
            PhoneVerified = false,
            LastLoginAt = null,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        });
    }
}