using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Base.Migrations
{
    /// <inheritdoc />
    public partial class SystemLogDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Action",
                table: "SystemLog",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AfterData",
                table: "SystemLog",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BeforeData",
                table: "SystemLog",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EntityId",
                table: "SystemLog",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                table: "SystemLog",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseData",
                table: "SystemLog",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Action",
                table: "SystemLog");

            migrationBuilder.DropColumn(
                name: "AfterData",
                table: "SystemLog");

            migrationBuilder.DropColumn(
                name: "BeforeData",
                table: "SystemLog");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "SystemLog");

            migrationBuilder.DropColumn(
                name: "EntityType",
                table: "SystemLog");

            migrationBuilder.DropColumn(
                name: "ResponseData",
                table: "SystemLog");
        }
    }
}
