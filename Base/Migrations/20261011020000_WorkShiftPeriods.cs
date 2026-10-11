using Base.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Base.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261011020000_WorkShiftPeriods")]
public partial class WorkShiftPeriods : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Period",
            table: "GoldShift",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "afternoon");

        migrationBuilder.Sql("""
            UPDATE "GoldShift"
            SET "Period" = CASE
                WHEN "Start" < TIME '12:00' AND "End" > TIME '12:00' THEN 'fullDay'
                WHEN "Start" < TIME '12:00' THEN 'morning'
                WHEN "Start" >= TIME '17:00' THEN 'evening'
                ELSE 'afternoon'
            END;
            """);

        migrationBuilder.AddCheckConstraint(
            name: "CK_GoldShift_Period",
            table: "GoldShift",
            sql: "\"Period\" IN ('morning','afternoon','evening','fullDay')");

    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint("CK_GoldShift_Period", "GoldShift");
        migrationBuilder.DropColumn(name: "Period", table: "GoldShift");
    }
}
