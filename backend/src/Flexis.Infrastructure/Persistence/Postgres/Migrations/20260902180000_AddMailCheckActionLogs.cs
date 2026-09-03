using System;
using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flexis.Infrastructure.Persistence.Postgres.Migrations
{
    [DbContext(typeof(FlexisDbContext))]
    [Migration("20260902180000_AddMailCheckActionLogs")]
    public partial class AddMailCheckActionLogs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mail_check_action_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    MailConnectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    MailboxEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    MailboxProvider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MessageId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FromAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Label = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Detail = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DurationMs = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mail_check_action_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mail_check_action_logs_mail_connections_MailConnectionId",
                        column: x => x.MailConnectionId,
                        principalTable: "mail_connections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_mail_check_action_logs_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_mail_check_action_logs_MailConnectionId",
                table: "mail_check_action_logs",
                column: "MailConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_mail_check_action_logs_RunId",
                table: "mail_check_action_logs",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_mail_check_action_logs_UserId_Action_OccurredAt",
                table: "mail_check_action_logs",
                columns: new[] { "UserId", "Action", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_mail_check_action_logs_UserId_MailConnectionId_OccurredAt",
                table: "mail_check_action_logs",
                columns: new[] { "UserId", "MailConnectionId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_mail_check_action_logs_UserId_OccurredAt",
                table: "mail_check_action_logs",
                columns: new[] { "UserId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_mail_check_action_logs_UserId_Source_OccurredAt",
                table: "mail_check_action_logs",
                columns: new[] { "UserId", "Source", "OccurredAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mail_check_action_logs");
        }
    }
}
