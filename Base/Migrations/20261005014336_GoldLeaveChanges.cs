using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Base.Migrations
{
    /// <inheritdoc />
    public partial class GoldLeaveChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCancellation",
                table: "GoldRequest",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ReplacesRequestId",
                table: "GoldRequest",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoldRequest_ReplacesRequestId",
                table: "GoldRequest",
                column: "ReplacesRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_GoldRequest_GoldRequest_ReplacesRequestId",
                table: "GoldRequest",
                column: "ReplacesRequestId",
                principalTable: "GoldRequest",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoldRequest_GoldRequest_ReplacesRequestId",
                table: "GoldRequest");

            migrationBuilder.DropIndex(
                name: "IX_GoldRequest_ReplacesRequestId",
                table: "GoldRequest");

            migrationBuilder.DropColumn(
                name: "IsCancellation",
                table: "GoldRequest");

            migrationBuilder.DropColumn(
                name: "ReplacesRequestId",
                table: "GoldRequest");
        }
    }
}
