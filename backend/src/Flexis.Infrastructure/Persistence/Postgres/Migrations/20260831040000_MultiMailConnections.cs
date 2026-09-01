using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flexis.Infrastructure.Persistence.Postgres.Migrations
{
    [DbContext(typeof(FlexisDbContext))]
    [Migration("20260831040000_MultiMailConnections")]
    public partial class MultiMailConnections : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_mail_connections_UserId",
                table: "mail_connections");

            migrationBuilder.CreateIndex(
                name: "IX_mail_connections_UserId",
                table: "mail_connections",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_mail_connections_UserId_Provider_ExternalSubject",
                table: "mail_connections",
                columns: new[] { "UserId", "Provider", "ExternalSubject" },
                unique: true);

            migrationBuilder.DropIndex(
                name: "IX_mail_check_processed_messages_UserId_GmailMessageId",
                table: "mail_check_processed_messages");

            migrationBuilder.RenameColumn(
                name: "GmailMessageId",
                table: "mail_check_processed_messages",
                newName: "MessageId");

            migrationBuilder.AddColumn<Guid>(
                name: "MailConnectionId",
                table: "mail_check_processed_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE mail_check_processed_messages AS processed
                SET "MailConnectionId" = connection."Id"
                FROM mail_connections AS connection
                WHERE connection."UserId" = processed."UserId";
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM mail_check_processed_messages
                WHERE "MailConnectionId" IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "MailConnectionId",
                table: "mail_check_processed_messages",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_mail_check_processed_messages_MailConnectionId_MessageId",
                table: "mail_check_processed_messages",
                columns: new[] { "MailConnectionId", "MessageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mail_check_processed_messages_UserId",
                table: "mail_check_processed_messages",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_mail_check_processed_messages_mail_connections_MailConnectionId",
                table: "mail_check_processed_messages",
                column: "MailConnectionId",
                principalTable: "mail_connections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_mail_check_processed_messages_mail_connections_MailConnectionId",
                table: "mail_check_processed_messages");

            migrationBuilder.DropIndex(
                name: "IX_mail_check_processed_messages_UserId",
                table: "mail_check_processed_messages");

            migrationBuilder.DropIndex(
                name: "IX_mail_check_processed_messages_MailConnectionId_MessageId",
                table: "mail_check_processed_messages");

            migrationBuilder.DropColumn(
                name: "MailConnectionId",
                table: "mail_check_processed_messages");

            migrationBuilder.RenameColumn(
                name: "MessageId",
                table: "mail_check_processed_messages",
                newName: "GmailMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_mail_check_processed_messages_UserId_GmailMessageId",
                table: "mail_check_processed_messages",
                columns: new[] { "UserId", "GmailMessageId" },
                unique: true);

            migrationBuilder.DropIndex(
                name: "IX_mail_connections_UserId_Provider_ExternalSubject",
                table: "mail_connections");

            migrationBuilder.DropIndex(
                name: "IX_mail_connections_UserId",
                table: "mail_connections");

            migrationBuilder.CreateIndex(
                name: "IX_mail_connections_UserId",
                table: "mail_connections",
                column: "UserId",
                unique: true);
        }
    }
}
