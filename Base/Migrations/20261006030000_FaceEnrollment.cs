using Base.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Base.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261006030000_FaceEnrollment")]
public partial class FaceEnrollment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "FaceEnrollmentId", table: "GoldEmployee", type: "character varying(100)", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "FaceRegisteredAt", table: "GoldEmployee", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "FaceRegisteredBy", table: "GoldEmployee", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<byte[]>(name: "FacePhoto", table: "GoldAttendance", type: "bytea", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "FaceEnrollmentId", table: "GoldEmployee");
        migrationBuilder.DropColumn(name: "FaceRegisteredAt", table: "GoldEmployee");
        migrationBuilder.DropColumn(name: "FaceRegisteredBy", table: "GoldEmployee");
        migrationBuilder.DropColumn(name: "FacePhoto", table: "GoldAttendance");
    }
}
