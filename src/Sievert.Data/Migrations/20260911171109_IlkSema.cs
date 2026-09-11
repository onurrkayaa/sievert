using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Sievert.Data.Migrations
{
    /// <inheritdoc />
    public partial class IlkSema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Repositories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    RemoteUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TotalCommits = table.Column<int>(type: "integer", nullable: false),
                    FirstCommitDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastCommitDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ScannedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ScannedSha = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Repositories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Commits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RepositoryId = table.Column<int>(type: "integer", nullable: false),
                    Sha = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AuthorName = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    AuthorEmail = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    AuthorDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MessageSubject = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    MessageFull = table.Column<string>(type: "text", nullable: false),
                    ParentCount = table.Column<int>(type: "integer", nullable: false),
                    IsBot = table.Column<bool>(type: "boolean", nullable: false),
                    CoAuthorCount = table.Column<int>(type: "integer", nullable: false),
                    LinesAdded = table.Column<int>(type: "integer", nullable: false),
                    LinesDeleted = table.Column<int>(type: "integer", nullable: false),
                    ChangedFiles = table.Column<int>(type: "integer", nullable: false),
                    ChangedCSharpFiles = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Commits_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommitFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CommitId = table.Column<int>(type: "integer", nullable: false),
                    Path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OldPath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LinesAdded = table.Column<int>(type: "integer", nullable: false),
                    LinesDeleted = table.Column<int>(type: "integer", nullable: false),
                    ChangeKind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsCSharp = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommitFiles_Commits_CommitId",
                        column: x => x.CommitId,
                        principalTable: "Commits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommitMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CommitId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommitMetrics_Commits_CommitId",
                        column: x => x.CommitId,
                        principalTable: "Commits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommitFiles_CommitId",
                table: "CommitFiles",
                column: "CommitId");

            migrationBuilder.CreateIndex(
                name: "IX_CommitFiles_Path",
                table: "CommitFiles",
                column: "Path");

            migrationBuilder.CreateIndex(
                name: "IX_CommitMetrics_CommitId",
                table: "CommitMetrics",
                column: "CommitId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Commits_AuthorDateUtc",
                table: "Commits",
                column: "AuthorDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Commits_RepositoryId_Sha",
                table: "Commits",
                columns: new[] { "RepositoryId", "Sha" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Repositories_Name",
                table: "Repositories",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommitFiles");

            migrationBuilder.DropTable(
                name: "CommitMetrics");

            migrationBuilder.DropTable(
                name: "Commits");

            migrationBuilder.DropTable(
                name: "Repositories");
        }
    }
}
