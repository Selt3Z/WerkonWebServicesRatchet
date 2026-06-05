using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WerkonWebServicesRatchet.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitAssignedMechanic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedMechanicUserId",
                table: "Visits",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Visits_AssignedMechanicUserId",
                table: "Visits",
                column: "AssignedMechanicUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_AspNetUsers_AssignedMechanicUserId",
                table: "Visits",
                column: "AssignedMechanicUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Visits_AspNetUsers_AssignedMechanicUserId",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_Visits_AssignedMechanicUserId",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "AssignedMechanicUserId",
                table: "Visits");
        }
    }
}
