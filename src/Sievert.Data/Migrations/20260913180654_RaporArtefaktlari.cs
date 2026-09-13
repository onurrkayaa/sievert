using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sievert.Data.Migrations
{
    /// <inheritdoc />
    public partial class RaporArtefaktlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportArtifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalysisJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    RepositoryId = table.Column<int>(type: "integer", nullable: false),
                    RiskAnalysisJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    StaticAnalysisJobId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Culture = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SafeFileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ByteLength = table.Column<long>(type: "bigint", nullable: true),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ManifestJson = table.Column<string>(type: "text", nullable: false),
                    ManifestSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequestedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeneratedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    VerifiedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RequestFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsPartial = table.Column<bool>(type: "boolean", nullable: false),
                    PageCount = table.Column<int>(type: "integer", nullable: true),
                    SchemaVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GeneratorVersion = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportArtifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportArtifacts_AnalysisJobs_AnalysisJobId",
                        column: x => x.AnalysisJobId,
                        principalTable: "AnalysisJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportArtifacts_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportArtifacts_AnalysisJobId",
                table: "ReportArtifacts",
                column: "AnalysisJobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportArtifacts_RepositoryId_IdempotencyKey",
                table: "ReportArtifacts",
                columns: new[] { "RepositoryId", "IdempotencyKey" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportArtifacts_RepositoryId_RequestedAtUtc",
                table: "ReportArtifacts",
                columns: new[] { "RepositoryId", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportArtifacts_StorageKey",
                table: "ReportArtifacts",
                column: "StorageKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportArtifacts");
        }
    }
}
