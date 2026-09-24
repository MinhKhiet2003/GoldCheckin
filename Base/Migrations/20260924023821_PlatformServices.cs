using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Base.Migrations
{
    /// <inheritdoc />
    public partial class PlatformServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Link = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetToken",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetToken", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefreshToken",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DeviceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshToken", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSetting",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSetting", x => x.Key);
                });

            migrationBuilder.InsertData(
                table: "Permission",
                columns: new[] { "Code", "Name" },
                values: new object[,]
                {
                    { "dashboard.read", "Xem dashboard" },
                    { "notifications.read", "Xem thông báo" },
                    { "notifications.update", "Cập nhật thông báo" },
                    { "permissions.manage", "Quản lý quyền" },
                    { "roles.manage", "Quản lý vai trò" },
                    { "settings.manage", "Quản lý cấu hình hệ thống" }
                });

            migrationBuilder.InsertData(
                table: "RolePermission",
                columns: new[] { "PermissionCode", "Role" },
                values: new object[,]
                {
                    { "dashboard.read", "admin" },
                    { "notifications.read", "admin" },
                    { "notifications.update", "admin" },
                    { "permissions.manage", "admin" },
                    { "roles.manage", "admin" },
                    { "settings.manage", "admin" },
                    { "notifications.read", "user" },
                    { "notifications.update", "user" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notification_UserId_ReadAt_CreatedAt",
                table: "Notification",
                columns: new[] { "UserId", "ReadAt", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetToken_TokenHash",
                table: "PasswordResetToken",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetToken_UserId_ExpiresAt",
                table: "PasswordResetToken",
                columns: new[] { "UserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_TokenHash",
                table: "RefreshToken",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_UserId_ExpiresAt",
                table: "RefreshToken",
                columns: new[] { "UserId", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.DropTable(
                name: "PasswordResetToken");

            migrationBuilder.DropTable(
                name: "RefreshToken");

            migrationBuilder.DropTable(
                name: "SystemSetting");

            migrationBuilder.DeleteData(
                table: "RolePermission",
                keyColumns: new[] { "PermissionCode", "Role" },
                keyValues: new object[] { "dashboard.read", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermission",
                keyColumns: new[] { "PermissionCode", "Role" },
                keyValues: new object[] { "notifications.read", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermission",
                keyColumns: new[] { "PermissionCode", "Role" },
                keyValues: new object[] { "notifications.update", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermission",
                keyColumns: new[] { "PermissionCode", "Role" },
                keyValues: new object[] { "permissions.manage", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermission",
                keyColumns: new[] { "PermissionCode", "Role" },
                keyValues: new object[] { "roles.manage", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermission",
                keyColumns: new[] { "PermissionCode", "Role" },
                keyValues: new object[] { "settings.manage", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermission",
                keyColumns: new[] { "PermissionCode", "Role" },
                keyValues: new object[] { "notifications.read", "user" });

            migrationBuilder.DeleteData(
                table: "RolePermission",
                keyColumns: new[] { "PermissionCode", "Role" },
                keyValues: new object[] { "notifications.update", "user" });

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Code",
                keyValue: "dashboard.read");

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Code",
                keyValue: "notifications.read");

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Code",
                keyValue: "notifications.update");

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Code",
                keyValue: "permissions.manage");

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Code",
                keyValue: "roles.manage");

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Code",
                keyValue: "settings.manage");
        }
    }
}
