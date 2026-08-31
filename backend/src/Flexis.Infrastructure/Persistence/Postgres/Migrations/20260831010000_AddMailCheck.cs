using System;
using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flexis.Infrastructure.Persistence.Postgres.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(FlexisDbContext))]
    [Migration("20260831010000_AddMailCheck")]
    public partial class AddMailCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mail_check_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiKeyProtected = table.Column<string>(type: "text", nullable: true),
                    Model = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    LastRunAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LastLabeled = table.Column<int>(type: "integer", nullable: false),
                    LastTrashed = table.Column<int>(type: "integer", nullable: false),
                    LastSkipped = table.Column<int>(type: "integer", nullable: false),
                    LastProcessed = table.Column<int>(type: "integer", nullable: false),
                    LastErrors = table.Column<int>(type: "integer", nullable: false),
                    LastHasMore = table.Column<bool>(type: "boolean", nullable: false),
                    TotalLabeled = table.Column<int>(type: "integer", nullable: false),
                    TotalTrashed = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mail_check_settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mail_check_settings_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_mail_check_settings_UserId",
                table: "mail_check_settings",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateTable(
                name: "mail_check_processed_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GmailMessageId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Decision = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mail_check_processed_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mail_check_processed_messages_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_mail_check_processed_messages_UserId_GmailMessageId",
                table: "mail_check_processed_messages",
                columns: new[] { "UserId", "GmailMessageId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "mail_check_processed_messages");
            migrationBuilder.DropTable(name: "mail_check_settings");
        }
    }
}
