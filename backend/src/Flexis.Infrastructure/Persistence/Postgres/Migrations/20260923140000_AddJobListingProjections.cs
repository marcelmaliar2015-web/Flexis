using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flexis.Infrastructure.Persistence.Postgres.Migrations
{
    [DbContext(typeof(FlexisDbContext))]
    [Migration("20260923140000_AddJobListingProjections")]
    public partial class AddJobListingProjections : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "job_listing_projections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ArchiveTab = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ListingKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Position = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Link = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Jd = table.Column<string>(type: "text", nullable: false),
                    Download = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Issue = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ProfileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RowNumber = table.Column<int>(type: "integer", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_listing_projections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_listing_projections_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "job_sheet_sync_states",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpreadsheetId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DriveModifiedTime = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_sheet_sync_states", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_sheet_sync_states_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_listing_projections_UserId_ProfileId_Source",
                table: "job_listing_projections",
                columns: new[] { "UserId", "ProfileId", "Source" });

            migrationBuilder.CreateIndex(
                name: "IX_job_listing_projections_UserId_Source_ProfileId_ArchiveTab_ListingKey",
                table: "job_listing_projections",
                columns: new[] { "UserId", "Source", "ProfileId", "ArchiveTab", "ListingKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_sheet_sync_states_UserId_SpreadsheetId",
                table: "job_sheet_sync_states",
                columns: new[] { "UserId", "SpreadsheetId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "job_listing_projections");
            migrationBuilder.DropTable(name: "job_sheet_sync_states");
        }
    }
}
