using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sievert.Data.Migrations
{
    /// <inheritdoc />
    public partial class CommitMetrikleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AuthorCommitCount",
                table: "CommitMetrics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AuthorFileExperience",
                table: "CommitMetrics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CsFilesChanged",
                table: "CommitMetrics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DirectoryCount",
                table: "CommitMetrics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DistinctAuthorsOnFiles",
                table: "CommitMetrics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "Entropy",
                table: "CommitMetrics",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "FilesChanged",
                table: "CommitMetrics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsFix",
                table: "CommitMetrics",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LinesAdded",
                table: "CommitMetrics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LinesDeleted",
                table: "CommitMetrics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxFileAgeDays",
                table: "CommitMetrics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinFileAgeDays",
                table: "CommitMetrics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PriorChanges",
                table: "CommitMetrics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PriorFixes",
                table: "CommitMetrics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SubsystemCount",
                table: "CommitMetrics",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthorCommitCount",
                table: "CommitMetrics");

            migrationBuilder.DropColumn(
                name: "AuthorFileExperience",
                table: "CommitMetrics");

            migrationBuilder.DropColumn(
                name: "CsFilesChanged",
                table: "CommitMetrics");

            migrationBuilder.DropColumn(
                name: "DirectoryCount",
                table: "CommitMetrics");

            migrationBuilder.DropColumn(
                name: "DistinctAuthorsOnFiles",
                table: "CommitMetrics");

            migrationBuilder.DropColumn(
                name: "Entropy",
                table: "CommitMetrics");

            migrationBuilder.DropColumn(
                name: "FilesChanged",
                table: "CommitMetrics");

            migrationBuilder.DropColumn(
                name: "IsFix",
                table: "CommitMetrics");

            migrationBuilder.DropColumn(
                name: "LinesAdded",
                table: "CommitMetrics");

            migrationBuilder.DropColumn(
                name: "LinesDeleted",
                table: "CommitMetrics");

            migrationBuilder.DropColumn(
                name: "MaxFileAgeDays",
                table: "CommitMetrics");

            migrationBuilder.DropColumn(
                name: "MinFileAgeDays",
                table: "CommitMetrics");

            migrationBuilder.DropColumn(
                name: "PriorChanges",
                table: "CommitMetrics");

            migrationBuilder.DropColumn(
                name: "PriorFixes",
                table: "CommitMetrics");

            migrationBuilder.DropColumn(
                name: "SubsystemCount",
                table: "CommitMetrics");
        }
    }
}
