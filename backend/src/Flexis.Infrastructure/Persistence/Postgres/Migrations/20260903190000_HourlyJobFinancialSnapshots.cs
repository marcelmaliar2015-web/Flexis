using System;
using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flexis.Infrastructure.Persistence.Postgres.Migrations
{
    [DbContext(typeof(FlexisDbContext))]
    [Migration("20260903190000_HourlyJobFinancialSnapshots")]
    public partial class HourlyJobFinancialSnapshots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_job_financial_snapshots_UserId_CapturedOn",
                table: "job_financial_snapshots");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CapturedHour",
                table: "job_financial_snapshots",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.Zero));

            migrationBuilder.Sql(
                """
                UPDATE job_financial_snapshots
                SET "CapturedHour" = date_trunc('hour', "CapturedAt" AT TIME ZONE 'UTC') AT TIME ZONE 'UTC';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_job_financial_snapshots_UserId_CapturedHour",
                table: "job_financial_snapshots",
                columns: new[] { "UserId", "CapturedHour" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_financial_snapshots_UserId_CapturedOn",
                table: "job_financial_snapshots",
                columns: new[] { "UserId", "CapturedOn" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_job_financial_snapshots_UserId_CapturedHour",
                table: "job_financial_snapshots");

            migrationBuilder.DropIndex(
                name: "IX_job_financial_snapshots_UserId_CapturedOn",
                table: "job_financial_snapshots");

            migrationBuilder.DropColumn(
                name: "CapturedHour",
                table: "job_financial_snapshots");

            migrationBuilder.CreateIndex(
                name: "IX_job_financial_snapshots_UserId_CapturedOn",
                table: "job_financial_snapshots",
                columns: new[] { "UserId", "CapturedOn" },
                unique: true);
        }
    }
}
