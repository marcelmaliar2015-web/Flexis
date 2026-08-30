using System;
using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flexis.Infrastructure.Persistence.Postgres.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(FlexisDbContext))]
    [Migration("20260830213000_AddJobPipelineEntries")]
    public partial class AddJobPipelineEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "job_pipeline_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationSheetId = table.Column<int>(type: "integer", nullable: false),
                    LocationName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_pipeline_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_pipeline_entries_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_pipeline_entries_UserId_ProfileId_SourceId_LocationSheetId",
                table: "job_pipeline_entries",
                columns: new[] { "UserId", "ProfileId", "SourceId", "LocationSheetId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "job_pipeline_entries");
        }
    }
}
