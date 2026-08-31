using System;
using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flexis.Infrastructure.Persistence.Postgres.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(FlexisDbContext))]
    [Migration("20260830230000_AddJobPipelineBannedCompanies")]
    public partial class AddJobPipelineBannedCompanies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                        name: "FK_job_pipeline_banned_companies_job_pipeline_entries_PipelineEntryId",
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "job_pipeline_banned_companies");
        }
    }
}
