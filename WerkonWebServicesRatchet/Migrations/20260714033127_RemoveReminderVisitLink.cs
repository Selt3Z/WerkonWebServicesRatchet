using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WerkonWebServicesRatchet.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReminderVisitLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reminders_Visits_VisitId",
                table: "Reminders");

            migrationBuilder.DropIndex(
                name: "IX_Reminders_VisitId",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "VisitId",
                table: "Reminders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "VisitId",
                table: "Reminders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_VisitId",
                table: "Reminders",
                column: "VisitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reminders_Visits_VisitId",
                table: "Reminders",
                column: "VisitId",
                principalTable: "Visits",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
