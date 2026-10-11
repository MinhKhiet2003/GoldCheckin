using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Base.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFullDayShiftPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_GoldShift_Period",
                table: "GoldShift");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GoldDepartmentShift_Period",
                table: "GoldDepartmentShift");

            migrationBuilder.Sql("""
                UPDATE "GoldShift"
                SET "Period" = CASE
                    WHEN "Start" >= TIME '17:00' THEN 'evening'
                    WHEN "Start" >= TIME '12:00' THEN 'afternoon'
                    ELSE 'morning'
                END
                WHERE "Period" = 'fullDay';

                UPDATE "GoldDepartmentShift" AS departmentShift
                SET "Period" = shift."Period"
                FROM "GoldShift" AS shift
                WHERE departmentShift."ShiftId" = shift."Id";
                """);

            migrationBuilder.UpdateData(
                table: "GoldShift",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "Period",
                value: "morning");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GoldShift_Period",
                table: "GoldShift",
                sql: "\"Period\" IN ('morning','afternoon','evening')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GoldDepartmentShift_Period",
                table: "GoldDepartmentShift",
                sql: "\"Period\" IN ('morning','afternoon','evening')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_GoldShift_Period",
                table: "GoldShift");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GoldDepartmentShift_Period",
                table: "GoldDepartmentShift");

            migrationBuilder.UpdateData(
                table: "GoldShift",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "Period",
                value: "fullDay");

            migrationBuilder.Sql("""
                UPDATE "GoldShift"
                SET "Period" = 'fullDay'
                WHERE "Start" < TIME '12:00' AND "End" > TIME '12:00';

                UPDATE "GoldDepartmentShift" AS departmentShift
                SET "Period" = shift."Period"
                FROM "GoldShift" AS shift
                WHERE departmentShift."ShiftId" = shift."Id";
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_GoldShift_Period",
                table: "GoldShift",
                sql: "\"Period\" IN ('morning','afternoon','evening','fullDay')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GoldDepartmentShift_Period",
                table: "GoldDepartmentShift",
                sql: "\"Period\" IN ('morning','afternoon','evening','fullDay')");
        }
    }
}
