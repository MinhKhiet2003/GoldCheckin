using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Base.Migrations
{
    /// <inheritdoc />
    public partial class GoldLeaveSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "LeaveEnd",
                table: "GoldRequest",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(17, 0, 0));

            migrationBuilder.AddColumn<string>(
                name: "LeaveSession",
                table: "GoldRequest",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "full");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "LeaveStart",
                table: "GoldRequest",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(7, 30, 0));

            migrationBuilder.AddCheckConstraint(
                name: "CK_GoldLeaveSession",
                table: "GoldRequest",
                sql: "\"LeaveSession\" IN ('morning','afternoon','full')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_GoldLeaveSession",
                table: "GoldRequest");

            migrationBuilder.DropColumn(
                name: "LeaveEnd",
                table: "GoldRequest");

            migrationBuilder.DropColumn(
                name: "LeaveSession",
                table: "GoldRequest");

            migrationBuilder.DropColumn(
                name: "LeaveStart",
                table: "GoldRequest");
        }
    }
}
