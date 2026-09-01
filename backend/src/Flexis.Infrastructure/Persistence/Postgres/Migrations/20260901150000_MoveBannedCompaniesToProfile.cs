using System;
using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flexis.Infrastructure.Persistence.Postgres.Migrations
{
    [DbContext(typeof(FlexisDbContext))]
    [Migration("20260901150000_MoveBannedCompaniesToProfile")]
    public partial class MoveBannedCompaniesToProfile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "job_profile_banned_companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MatchKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_profile_banned_companies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_profile_banned_companies_job_catalog_items_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "job_catalog_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_profile_banned_companies_ProfileId_MatchKey",
                table: "job_profile_banned_companies",
                columns: new[] { "ProfileId", "MatchKey" },
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO job_profile_banned_companies ("Id", "ProfileId", "CompanyName", "MatchKey", "CreatedAt")
                SELECT DISTINCT ON (e."ProfileId", b."MatchKey")
                    b."Id",
                    e."ProfileId",
                    b."CompanyName",
                    b."MatchKey",
                    b."CreatedAt"
                FROM job_pipeline_banned_companies b
                INNER JOIN job_pipeline_entries e ON e."Id" = b."PipelineEntryId"
                ORDER BY e."ProfileId", b."MatchKey", b."CreatedAt"
                """);

            migrationBuilder.DropTable(name: "job_pipeline_banned_companies");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "job_pipeline_banned_companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PipelineEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MatchKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_pipeline_banned_companies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_pipeline_banned_companies_job_pipeline_entries_PipelineEn~",
                        column: x => x.PipelineEntryId,
                        principalTable: "job_pipeline_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_pipeline_banned_companies_PipelineEntryId_MatchKey",
                table: "job_pipeline_banned_companies",
                columns: new[] { "PipelineEntryId", "MatchKey" },
                unique: true);

            migrationBuilder.DropTable(name: "job_profile_banned_companies");
        }
    }
}
