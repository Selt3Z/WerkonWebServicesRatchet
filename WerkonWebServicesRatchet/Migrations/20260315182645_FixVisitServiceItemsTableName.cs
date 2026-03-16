using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WerkonWebServicesRatchet.Migrations
{
    /// <inheritdoc />
    public partial class FixVisitServiceItemsTableName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_visitServiceItems_Visits_VisitId",
                table: "visitServiceItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_visitServiceItems",
                table: "visitServiceItems");

            migrationBuilder.RenameTable(
                name: "visitServiceItems",
                newName: "VisitServiceItems");

            migrationBuilder.RenameIndex(
                name: "IX_visitServiceItems_VisitId",
                table: "VisitServiceItems",
                newName: "IX_VisitServiceItems_VisitId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VisitServiceItems",
                table: "VisitServiceItems",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VisitServiceItems_Visits_VisitId",
                table: "VisitServiceItems",
                column: "VisitId",
                principalTable: "Visits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VisitServiceItems_Visits_VisitId",
                table: "VisitServiceItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VisitServiceItems",
                table: "VisitServiceItems");

            migrationBuilder.RenameTable(
                name: "VisitServiceItems",
                newName: "visitServiceItems");

            migrationBuilder.RenameIndex(
                name: "IX_VisitServiceItems_VisitId",
                table: "visitServiceItems",
                newName: "IX_visitServiceItems_VisitId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_visitServiceItems",
                table: "visitServiceItems",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_visitServiceItems_Visits_VisitId",
                table: "visitServiceItems",
                column: "VisitId",
                principalTable: "Visits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
