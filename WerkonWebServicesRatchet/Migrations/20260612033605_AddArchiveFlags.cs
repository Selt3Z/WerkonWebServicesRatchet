using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WerkonWebServicesRatchet.Migrations
{
    /// <inheritdoc />
    public partial class AddArchiveFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAtUtc",
                table: "Visits",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Visits",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAtUtc",
                table: "Vehicles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Vehicles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAtUtc",
                table: "Clients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Clients",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Visits_IsArchived",
                table: "Visits",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_IsArchived",
                table: "Vehicles",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_IsArchived",
                table: "Clients",
                column: "IsArchived");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Visits_IsArchived",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_IsArchived",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Clients_IsArchived",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUtc",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUtc",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUtc",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Clients");
        }
    }
}
