using System;
using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flexis.Infrastructure.Persistence.Postgres.Migrations
{
    [DbContext(typeof(FlexisDbContext))]
    [Migration("20260901240000_AddMailCheckAutoCheck")]
    public partial class AddMailCheckAutoCheck : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoCheckEnabled",
                table: "mail_check_settings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "mail_check_scan_states",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MailConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckedUntilAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastScanAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ScanCaughtUp = table.Column<bool>(type: "boolean", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mail_check_scan_states", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mail_check_scan_states_mail_connections_MailConnectionId",
                        column: x => x.MailConnectionId,
                        principalTable: "mail_connections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_mail_check_scan_states_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_mail_check_scan_states_MailConnectionId",
                table: "mail_check_scan_states",
                column: "MailConnectionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mail_check_scan_states_UserId",
                table: "mail_check_scan_states",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mail_check_scan_states");

            migrationBuilder.DropColumn(
                name: "AutoCheckEnabled",
                table: "mail_check_settings");
        }
    }
}
