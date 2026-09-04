using System;
using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flexis.Infrastructure.Persistence.Postgres.Migrations
{
    [DbContext(typeof(FlexisDbContext))]
    [Migration("20260904140000_AddJobListingTracking")]
    public partial class AddJobListingTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MainApplied",
                table: "job_financial_snapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MainInterviews",
                table: "job_financial_snapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MainPrice",
                table: "job_financial_snapshots",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MainTotal",
                table: "job_financial_snapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE job_financial_snapshots
                SET "MainPrice" = "TodayPrice",
                    "MainTotal" = "TodayTotal",
                    "MainApplied" = "TodayApplied",
                    "MainInterviews" = "TodayInterviews";
                """);

            migrationBuilder.CreateTable(
                name: "job_listing_copy_batches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    PipelineEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CopiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AddedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_listing_copy_batches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_listing_copy_batches_job_catalog_items_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "job_catalog_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_job_listing_copy_batches_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "job_listing_status_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_listing_status_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_listing_status_events_job_catalog_items_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "job_catalog_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_job_listing_status_events_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "job_listing_status_states",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_listing_status_states", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_listing_status_states_job_catalog_items_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "job_catalog_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_job_listing_status_states_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "job_listing_copy_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_listing_copy_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_listing_copy_items_job_listing_copy_batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "job_listing_copy_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_listing_copy_batches_ProfileId",
                table: "job_listing_copy_batches",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_job_listing_copy_batches_UserId_ProfileId_CopiedAt",
                table: "job_listing_copy_batches",
                columns: new[] { "UserId", "ProfileId", "CopiedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_job_listing_copy_items_BatchId_ListingKey",
                table: "job_listing_copy_items",
                columns: new[] { "BatchId", "ListingKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_listing_status_events_ProfileId",
                table: "job_listing_status_events",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_job_listing_status_events_UserId_OccurredAt",
                table: "job_listing_status_events",
                columns: new[] { "UserId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_job_listing_status_events_UserId_ProfileId_OccurredAt",
                table: "job_listing_status_events",
                columns: new[] { "UserId", "ProfileId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_job_listing_status_states_ProfileId",
                table: "job_listing_status_states",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_job_listing_status_states_UserId_ProfileId_ListingKey",
                table: "job_listing_status_states",
                columns: new[] { "UserId", "ProfileId", "ListingKey" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "job_listing_copy_items");
            migrationBuilder.DropTable(name: "job_listing_status_events");
            migrationBuilder.DropTable(name: "job_listing_status_states");
            migrationBuilder.DropTable(name: "job_listing_copy_batches");

            migrationBuilder.DropColumn(name: "MainApplied", table: "job_financial_snapshots");
            migrationBuilder.DropColumn(name: "MainInterviews", table: "job_financial_snapshots");
            migrationBuilder.DropColumn(name: "MainPrice", table: "job_financial_snapshots");
            migrationBuilder.DropColumn(name: "MainTotal", table: "job_financial_snapshots");
        }
    }
}
