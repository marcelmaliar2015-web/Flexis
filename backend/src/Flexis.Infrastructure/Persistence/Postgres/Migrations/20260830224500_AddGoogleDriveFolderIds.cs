using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flexis.Infrastructure.Persistence.Postgres.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(FlexisDbContext))]
    [Migration("20260830224500_AddGoogleDriveFolderIds")]
    public partial class AddGoogleDriveFolderIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DriveProfilesFolderId",
                table: "google_connections",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriveRootFolderId",
                table: "google_connections",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriveSourcesFolderId",
                table: "google_connections",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriveWorkspaceFolderId",
                table: "google_connections",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DriveProfilesFolderId",
                table: "google_connections");

            migrationBuilder.DropColumn(
                name: "DriveRootFolderId",
                table: "google_connections");

            migrationBuilder.DropColumn(
                name: "DriveSourcesFolderId",
                table: "google_connections");

            migrationBuilder.DropColumn(
                name: "DriveWorkspaceFolderId",
                table: "google_connections");
        }
    }
}
