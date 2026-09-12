using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Sievert.Data.Migrations
{
    /// <inheritdoc />
    public partial class ArkaPlanAnalizIsleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalysisJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RepositoryId = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RequestedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancellationRequestedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CurrentPhase = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    ProcessedItems = table.Column<int>(type: "integer", nullable: false),
                    TotalItems = table.Column<int>(type: "integer", nullable: true),
                    ProgressPercent = table.Column<double>(type: "double precision", nullable: true),
                    HeartbeatAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ActiveDeduplicationKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ResultCount = table.Column<int>(type: "integer", nullable: false),
                    IsResultComplete = table.Column<bool>(type: "boolean", nullable: false),
                    WorkerInstanceId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalysisJobs_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommitRiskSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AnalysisJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommitId = table.Column<int>(type: "integer", nullable: false),
                    RawModelScore = table.Column<double>(type: "double precision", nullable: false),
                    RiskIndex = table.Column<double>(type: "double precision", nullable: false),
                    DecisionAt05 = table.Column<bool>(type: "boolean", nullable: false),
                    DecisionAtTrainThreshold = table.Column<bool>(type: "boolean", nullable: false),
                    TrainThreshold = table.Column<double>(type: "double precision", nullable: false),
                    ModelProfile = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ModelChecksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsCalibrated = table.Column<bool>(type: "boolean", nullable: false),
                    WarningCodes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitRiskSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommitRiskSnapshots_AnalysisJobs_AnalysisJobId",
                        column: x => x.AnalysisJobId,
                        principalTable: "AnalysisJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommitRiskSnapshots_Commits_CommitId",
                        column: x => x.CommitId,
                        principalTable: "Commits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StaticAnalysisFindings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AnalysisJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Severity = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    RelativePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Line = table.Column<int>(type: "integer", nullable: false),
                    Column = table.Column<int>(type: "integer", nullable: true),
                    MemberName = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Rationale = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsTestCode = table.Column<bool>(type: "boolean", nullable: false),
                    IsSuppressed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaticAnalysisFindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaticAnalysisFindings_AnalysisJobs_AnalysisJobId",
                        column: x => x.AnalysisJobId,
                        principalTable: "AnalysisJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisJobs_ActiveDeduplicationKey",
                table: "AnalysisJobs",
                column: "ActiveDeduplicationKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisJobs_IdempotencyKey",
                table: "AnalysisJobs",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisJobs_RepositoryId_RequestedAtUtc",
                table: "AnalysisJobs",
                columns: new[] { "RepositoryId", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisJobs_Status",
                table: "AnalysisJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CommitRiskSnapshots_AnalysisJobId_CommitId",
                table: "CommitRiskSnapshots",
                columns: new[] { "AnalysisJobId", "CommitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommitRiskSnapshots_AnalysisJobId_RawModelScore",
                table: "CommitRiskSnapshots",
                columns: new[] { "AnalysisJobId", "RawModelScore" });

            migrationBuilder.CreateIndex(
                name: "IX_CommitRiskSnapshots_CommitId",
                table: "CommitRiskSnapshots",
                column: "CommitId");

            migrationBuilder.CreateIndex(
                name: "IX_StaticAnalysisFindings_AnalysisJobId_RuleCode",
                table: "StaticAnalysisFindings",
                columns: new[] { "AnalysisJobId", "RuleCode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommitRiskSnapshots");

            migrationBuilder.DropTable(
                name: "StaticAnalysisFindings");

            migrationBuilder.DropTable(
                name: "AnalysisJobs");
        }
    }
}
