using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flexis.Infrastructure.Persistence.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "google_connections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoogleSubject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    GoogleEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RefreshTokenProtected = table.Column<string>(type: "text", nullable: false),
                    AccessTokenProtected = table.Column<string>(type: "text", nullable: false),
                    AccessTokenExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GrantedScopes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ConnectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_google_connections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_google_connections_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_google_connections_UserId",
                table: "google_connections",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "google_connections");
        }
    }
}
