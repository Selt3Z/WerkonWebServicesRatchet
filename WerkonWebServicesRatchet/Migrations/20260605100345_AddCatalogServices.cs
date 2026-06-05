using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WerkonWebServicesRatchet.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CatalogServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DefaultUnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    DefaultUnit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogServices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogServices_Category",
                table: "CatalogServices",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogServices_Code",
                table: "CatalogServices",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogServices_Name",
                table: "CatalogServices",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogServices");
        }
    }
}
