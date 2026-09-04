using System;
using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flexis.Infrastructure.Persistence.Postgres.Migrations
{
    [DbContext(typeof(FlexisDbContext))]
    [Migration("20260904120000_AddJobProfileStatisticsSnapshots")]
    public partial class AddJobProfileStatisticsSnapshots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "job_profile_statistics_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileTitle = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CapturedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    CapturedHour = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Applied = table.Column<int>(type: "integer", nullable: false),
                    Interviews = table.Column<int>(type: "integer", nullable: false),
                    Unapplied = table.Column<int>(type: "integer", nullable: false),
                    Total = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_profile_statistics_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_profile_statistics_snapshots_job_catalog_items_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "job_catalog_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_job_profile_statistics_snapshots_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_profile_statistics_snapshots_UserId_CapturedHour",
                table: "job_profile_statistics_snapshots",
                columns: new[] { "UserId", "CapturedHour" });

            migrationBuilder.CreateIndex(
                name: "IX_job_profile_statistics_snapshots_UserId_ProfileId_CapturedHour",
                table: "job_profile_statistics_snapshots",
                columns: new[] { "UserId", "ProfileId", "CapturedHour" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_profile_statistics_snapshots_ProfileId",
                table: "job_profile_statistics_snapshots",
                column: "ProfileId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "job_profile_statistics_snapshots");
        }
    }
}
