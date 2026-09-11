using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sievert.Data.Migrations
{
    /// <inheritdoc />
    public partial class DepoKimligi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Repositories_Name",
                table: "Repositories");

            migrationBuilder.AddColumn<string>(
                name: "Identity",
                table: "Repositories",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IdentitySource",
                table: "Repositories",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Repositories_Identity",
                table: "Repositories",
                column: "Identity",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Repositories_Identity",
                table: "Repositories");

            migrationBuilder.DropColumn(
                name: "Identity",
                table: "Repositories");

            migrationBuilder.DropColumn(
                name: "IdentitySource",
                table: "Repositories");

            migrationBuilder.CreateIndex(
                name: "IX_Repositories_Name",
                table: "Repositories",
                column: "Name");
        }
    }
}
