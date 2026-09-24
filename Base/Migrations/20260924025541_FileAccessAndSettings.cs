using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Base.Migrations
{
    /// <inheritdoc />
    public partial class FileAccessAndSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Permission",
                columns: new[] { "Code", "Name" },
                values: new object[,]
                {
                    { "files.delete", "Xóa file" },
                    { "files.read", "Xem và tải file" }
                });

            migrationBuilder.InsertData(
                table: "RolePermission",
                columns: new[] { "PermissionCode", "Role" },
                values: new object[,]
                {
                    { "files.delete", "admin" },
                    { "files.read", "admin" },
                    { "files.delete", "user" },
                    { "files.read", "user" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermission",
                keyColumns: new[] { "PermissionCode", "Role" },
                keyValues: new object[] { "files.delete", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermission",
                keyColumns: new[] { "PermissionCode", "Role" },
                keyValues: new object[] { "files.read", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermission",
                keyColumns: new[] { "PermissionCode", "Role" },
                keyValues: new object[] { "files.delete", "user" });

            migrationBuilder.DeleteData(
                table: "RolePermission",
                keyColumns: new[] { "PermissionCode", "Role" },
                keyValues: new object[] { "files.read", "user" });

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Code",
                keyValue: "files.delete");

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Code",
                keyValue: "files.read");
        }
    }
}
