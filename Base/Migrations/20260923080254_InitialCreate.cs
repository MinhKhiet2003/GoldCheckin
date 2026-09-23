using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Base.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Permission",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permission", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "user"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EmailVerified = table.Column<bool>(type: "boolean", nullable: false),
                    PhoneVerified = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                    table.CheckConstraint("CK_User_Role", "\"Role\" IN ('admin', 'user')");
                });

            migrationBuilder.CreateTable(
                name: "RolePermission",
                columns: table => new
                {
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PermissionCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermission", x => new { x.Role, x.PermissionCode });
                    table.CheckConstraint("CK_RolePermission_Role", "\"Role\" IN ('admin', 'user')");
                    table.ForeignKey(
                        name: "FK_RolePermission_Permission_PermissionCode",
                        column: x => x.PermissionCode,
                        principalTable: "Permission",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Permission",
                columns: new[] { "Code", "Name" },
                values: new object[,]
                {
                    { "users.delete", "Xóa tài khoản" },
                    { "users.list", "Xem danh sách tài khoản" },
                    { "users.profile.read", "Xem hồ sơ của mình" },
                    { "users.profile.update", "Cập nhật hồ sơ của mình" },
                    { "users.read", "Xem chi tiết tài khoản" },
                    { "users.update", "Cập nhật tài khoản" }
                });

            migrationBuilder.InsertData(
                table: "User",
                columns: new[] { "Id", "Address", "AvatarUrl", "CreatedAt", "DateOfBirth", "Email", "EmailVerified", "FullName", "Gender", "IsActive", "LastLoginAt", "PasswordHash", "PhoneNumber", "PhoneVerified", "Role", "UpdatedAt", "Username" },
                values: new object[] { new Guid("8f5da64b-8e27-4ba4-a8e0-11d088da7931"), "Việt Nam", null, new DateTime(2026, 9, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, "admin@example.com", false, "Quản trị viên hệ thống", "PreferNotToSay", true, null, "AQAAAAIAAYagAAAAEMGbm6CRE8hR33ytf+bnxKnv92rxeYIkloGWwVTkLYNvsqcxBEZQJWTql0+Tjp2JbQ==", null, false, "admin", new DateTime(2026, 9, 23, 0, 0, 0, 0, DateTimeKind.Utc), "admin" });

            migrationBuilder.InsertData(
                table: "RolePermission",
                columns: new[] { "PermissionCode", "Role" },
                values: new object[,]
                {
                    { "users.delete", "admin" },
                    { "users.list", "admin" },
                    { "users.profile.read", "admin" },
                    { "users.profile.update", "admin" },
                    { "users.read", "admin" },
                    { "users.update", "admin" },
                    { "users.profile.read", "user" },
                    { "users.profile.update", "user" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_RolePermission_PermissionCode",
                table: "RolePermission",
                column: "PermissionCode");

            migrationBuilder.CreateIndex(
                name: "IX_User_Email",
                table: "User",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_Username",
                table: "User",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolePermission");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Permission");
        }
    }
}
