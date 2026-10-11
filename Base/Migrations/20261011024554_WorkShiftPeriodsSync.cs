using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Base.Migrations
{
    /// <inheritdoc />
    public partial class WorkShiftPeriodsSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_GoldDepartmentShift_Period",
                table: "GoldDepartmentShift");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GoldDepartmentShift_Period",
                table: "GoldDepartmentShift",
                sql: "\"Period\" IN ('morning','afternoon','evening','fullDay')");

            migrationBuilder.Sql("""
                UPDATE "GoldDepartmentShift" AS department_shift
                SET "Period" = shift."Period"
                FROM "GoldShift" AS shift
                WHERE department_shift."ShiftId" = shift."Id";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_GoldDepartmentShift_Period",
                table: "GoldDepartmentShift");

            migrationBuilder.Sql("""
                UPDATE "GoldDepartmentShift" AS department_shift
                SET "Period" = 'afternoon'
                FROM "GoldShift" AS shift
                WHERE department_shift."ShiftId" = shift."Id"
                  AND department_shift."Period" = 'evening';
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_GoldDepartmentShift_Period",
                table: "GoldDepartmentShift",
                sql: "\"Period\" IN ('morning','afternoon','fullDay')");
        }
    }
}
