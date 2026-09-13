using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sievert.Data.Migrations
{
    /// <inheritdoc />
    public partial class AnalizKaynakDurumu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SourceCommitChangedDuringAnalysis",
                table: "AnalysisJobs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SourceDirtyFileCount",
                table: "AnalysisJobs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceHeadSha",
                table: "AnalysisJobs",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceHeadShortSha",
                table: "AnalysisJobs",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceRepositoryIdentity",
                table: "AnalysisJobs",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SourceStateCheckedAtUtc",
                table: "AnalysisJobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SourceStateVerifiedAtUtc",
                table: "AnalysisJobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceTreeState",
                table: "AnalysisJobs",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceCommitChangedDuringAnalysis",
                table: "AnalysisJobs");

            migrationBuilder.DropColumn(
                name: "SourceDirtyFileCount",
                table: "AnalysisJobs");

            migrationBuilder.DropColumn(
                name: "SourceHeadSha",
                table: "AnalysisJobs");

            migrationBuilder.DropColumn(
                name: "SourceHeadShortSha",
                table: "AnalysisJobs");

            migrationBuilder.DropColumn(
                name: "SourceRepositoryIdentity",
                table: "AnalysisJobs");

            migrationBuilder.DropColumn(
                name: "SourceStateCheckedAtUtc",
                table: "AnalysisJobs");

            migrationBuilder.DropColumn(
                name: "SourceStateVerifiedAtUtc",
                table: "AnalysisJobs");

            migrationBuilder.DropColumn(
                name: "SourceTreeState",
                table: "AnalysisJobs");
        }
    }
}
