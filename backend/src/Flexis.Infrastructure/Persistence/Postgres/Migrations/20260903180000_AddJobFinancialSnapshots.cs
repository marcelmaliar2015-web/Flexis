using System;
using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flexis.Infrastructure.Persistence.Postgres.Migrations
{
    [DbContext(typeof(FlexisDbContext))]
    [Migration("20260903180000_AddJobFinancialSnapshots")]
    public partial class AddJobFinancialSnapshots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "job_financial_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CapturedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TodayPrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    TodayTotal = table.Column<int>(type: "integer", nullable: false),
                    TodayApplied = table.Column<int>(type: "integer", nullable: false),
                    TodayInterviews = table.Column<int>(type: "integer", nullable: false),
                    ArchivedPrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    ArchivedTotal = table.Column<int>(type: "integer", nullable: false),
                    ArchivedApplied = table.Column<int>(type: "integer", nullable: false),
                    ArchivedInterviews = table.Column<int>(type: "integer", nullable: false),
                    LifetimePrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    LifetimeTotal = table.Column<int>(type: "integer", nullable: false),
                    LifetimeApplied = table.Column<int>(type: "integer", nullable: false),
                    LifetimeInterviews = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_financial_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_financial_snapshots_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_financial_snapshots_UserId_CapturedOn",
                table: "job_financial_snapshots",
                columns: new[] { "UserId", "CapturedOn" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "job_financial_snapshots");
        }
    }
}
