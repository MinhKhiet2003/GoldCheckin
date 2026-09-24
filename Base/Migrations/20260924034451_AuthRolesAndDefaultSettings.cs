using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Base.Migrations
{
    /// <inheritdoc />
    public partial class AuthRolesAndDefaultSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "SystemSetting",
                columns: new[] { "Key", "Description", "UpdatedAt", "Value" },
                values: new object[,]
                {
                    { "organization.address", "Địa chỉ đơn vị", new DateTime(2026, 9, 24, 0, 0, 0, 0, DateTimeKind.Utc), "" },
                    { "organization.email", "Email liên hệ", new DateTime(2026, 9, 24, 0, 0, 0, 0, DateTimeKind.Utc), "" },
                    { "organization.name", "Tên đơn vị hoặc doanh nghiệp", new DateTime(2026, 9, 24, 0, 0, 0, 0, DateTimeKind.Utc), "" },
                    { "organization.phone", "Số điện thoại liên hệ", new DateTime(2026, 9, 24, 0, 0, 0, 0, DateTimeKind.Utc), "" },
                    { "organization.taxCode", "Mã số thuế", new DateTime(2026, 9, 24, 0, 0, 0, 0, DateTimeKind.Utc), "" },
                    { "organization.website", "Website đơn vị", new DateTime(2026, 9, 24, 0, 0, 0, 0, DateTimeKind.Utc), "" },
                    { "system.faviconUrl", "Đường dẫn favicon hệ thống", new DateTime(2026, 9, 24, 0, 0, 0, 0, DateTimeKind.Utc), "" },
                    { "system.logoUrl", "Đường dẫn logo hệ thống", new DateTime(2026, 9, 24, 0, 0, 0, 0, DateTimeKind.Utc), "" },
                    { "system.name", "Tên hiển thị của hệ thống", new DateTime(2026, 9, 24, 0, 0, 0, 0, DateTimeKind.Utc), "Base API" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SystemSetting",
                keyColumn: "Key",
                keyValue: "organization.address");

            migrationBuilder.DeleteData(
                table: "SystemSetting",
                keyColumn: "Key",
                keyValue: "organization.email");

            migrationBuilder.DeleteData(
                table: "SystemSetting",
                keyColumn: "Key",
                keyValue: "organization.name");

            migrationBuilder.DeleteData(
                table: "SystemSetting",
                keyColumn: "Key",
                keyValue: "organization.phone");

            migrationBuilder.DeleteData(
                table: "SystemSetting",
                keyColumn: "Key",
                keyValue: "organization.taxCode");

            migrationBuilder.DeleteData(
                table: "SystemSetting",
                keyColumn: "Key",
                keyValue: "organization.website");

            migrationBuilder.DeleteData(
                table: "SystemSetting",
                keyColumn: "Key",
                keyValue: "system.faviconUrl");

            migrationBuilder.DeleteData(
                table: "SystemSetting",
                keyColumn: "Key",
                keyValue: "system.logoUrl");

            migrationBuilder.DeleteData(
                table: "SystemSetting",
                keyColumn: "Key",
                keyValue: "system.name");
        }
    }
}
