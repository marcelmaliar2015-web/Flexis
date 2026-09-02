using System;
using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flexis.Infrastructure.Persistence.Postgres.Migrations
{
    [DbContext(typeof(FlexisDbContext))]
    [Migration("20260901250000_AddMailCheckScanNewestAt")]
    public partial class AddMailCheckScanNewestAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CheckedNewestAt",
                table: "mail_check_scan_states",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE mail_check_scan_states
                SET "CheckedNewestAt" = "CheckedUntilAt"
                WHERE "CheckedNewestAt" IS NULL AND "CheckedUntilAt" IS NOT NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckedNewestAt",
                table: "mail_check_scan_states");
        }
    }
}
