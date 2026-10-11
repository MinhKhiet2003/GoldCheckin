using Base.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Base.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261011010000_DepartmentShiftOptions")]
public partial class DepartmentShiftOptions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "GoldDepartmentShift",
            columns: table => new
            {
                DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                ShiftId = table.Column<Guid>(type: "uuid", nullable: false),
                Period = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GoldDepartmentShift", x => new { x.DepartmentId, x.ShiftId });
                table.ForeignKey("FK_GoldDepartmentShift_GoldDepartment_DepartmentId", x => x.DepartmentId,
                    "GoldDepartment", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_GoldDepartmentShift_GoldShift_ShiftId", x => x.ShiftId,
                    "GoldShift", "Id", onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("CK_GoldDepartmentShift_Period", "\"Period\" IN ('morning','afternoon','fullDay')");
            });

        migrationBuilder.CreateIndex(
            name: "IX_GoldDepartmentShift_ShiftId",
            table: "GoldDepartmentShift",
            column: "ShiftId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "GoldDepartmentShift");
    }
}
