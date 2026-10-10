using Base.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Base.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261010100000_MultiPunchSupplement")]
public partial class MultiPunchSupplement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "SupplementPunchesJson", table: "GoldRequest", type: "jsonb", nullable: false, defaultValue: "[]");
        migrationBuilder.DropIndex(name: "IX_GoldAttendance_EmployeeId_Date_Type", table: "GoldAttendance");
        migrationBuilder.CreateIndex(name: "IX_GoldAttendance_EmployeeId_Date_Type_RecordedAt", table: "GoldAttendance", columns: new[] { "EmployeeId", "Date", "Type", "RecordedAt" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_GoldAttendance_EmployeeId_Date_Type_RecordedAt", table: "GoldAttendance");
        migrationBuilder.CreateIndex(name: "IX_GoldAttendance_EmployeeId_Date_Type", table: "GoldAttendance", columns: new[] { "EmployeeId", "Date", "Type" }, unique: true);
        migrationBuilder.DropColumn(name: "SupplementPunchesJson", table: "GoldRequest");
    }
}
