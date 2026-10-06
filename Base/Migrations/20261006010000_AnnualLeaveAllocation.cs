using Base.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Base.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261006010000_AnnualLeaveAllocation")]
public partial class AnnualLeaveAllocation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(name: "AnnualLeaveBalance", table: "GoldEmployee", type: "numeric(6,2)", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<decimal>(name: "PaidLeaveDays", table: "GoldRequest", type: "numeric(6,2)", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<decimal>(name: "UnpaidLeaveDays", table: "GoldRequest", type: "numeric(6,2)", nullable: false, defaultValue: 0m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "AnnualLeaveBalance", table: "GoldEmployee");
        migrationBuilder.DropColumn(name: "PaidLeaveDays", table: "GoldRequest");
        migrationBuilder.DropColumn(name: "UnpaidLeaveDays", table: "GoldRequest");
    }
}
