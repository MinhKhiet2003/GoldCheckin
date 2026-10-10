using Base.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Base.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261010090000_ExplanationAttendanceTimestamps")]
public partial class ExplanationAttendanceTimestamps : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(name: "SystemRecordedAt", table: "GoldRequest", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<DateOnly>(name: "SystemRecordedDate", table: "GoldRequest", type: "date", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "SystemRecordedAt", table: "GoldRequest");
        migrationBuilder.DropColumn(name: "SystemRecordedDate", table: "GoldRequest");
    }
}
