using Base.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Base.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261006020000_EmployeeStartDate")]
public partial class EmployeeStartDate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateOnly>(
            name: "EmploymentStartDate",
            table: "GoldEmployee",
            type: "date",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "EmploymentStartDate", table: "GoldEmployee");
    }
}
