using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sievert.Data.Migrations
{
    /// <inheritdoc />
    public partial class SzzEtiketleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LocalPath",
                table: "Repositories",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBugIntroducing",
                table: "Commits",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LabelSource",
                table: "Commits",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Commits_IsBugIntroducing",
                table: "Commits",
                column: "IsBugIntroducing");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Commits_IsBugIntroducing",
                table: "Commits");

            migrationBuilder.DropColumn(
                name: "LocalPath",
                table: "Repositories");

            migrationBuilder.DropColumn(
                name: "IsBugIntroducing",
                table: "Commits");

            migrationBuilder.DropColumn(
                name: "LabelSource",
                table: "Commits");
        }
    }
}
