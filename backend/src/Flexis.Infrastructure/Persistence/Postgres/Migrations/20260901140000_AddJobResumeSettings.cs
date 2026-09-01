using System;
using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flexis.Infrastructure.Persistence.Postgres.Migrations
{
    [DbContext(typeof(FlexisDbContext))]
    [Migration("20260901140000_AddJobResumeSettings")]
    public partial class AddJobResumeSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "job_resume_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerOptionsJson = table.Column<string>(type: "text", nullable: false),
                    JobMasterSpreadsheetId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    JobMasterUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_resume_settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_resume_settings_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_resume_settings_UserId",
                table: "job_resume_settings",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateTable(
                name: "job_profile_resume_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Prompt = table.Column<string>(type: "text", nullable: false),
                    ResumeStyle = table.Column<int>(type: "integer", nullable: true),
                    Owner = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_profile_resume_settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_profile_resume_settings_job_catalog_items_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "job_catalog_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_profile_resume_settings_ProfileId",
                table: "job_profile_resume_settings",
                column: "ProfileId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "job_profile_resume_settings");
            migrationBuilder.DropTable(name: "job_resume_settings");
        }
    }
}
