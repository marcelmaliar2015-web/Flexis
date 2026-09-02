using System;
using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flexis.Infrastructure.Persistence.Postgres.Migrations
{
    [DbContext(typeof(FlexisDbContext))]
    [Migration("20260901220000_AddMailCheckLabelActions")]
    public partial class AddMailCheckLabelActions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Decision",
                table: "mail_check_processed_messages",
                newName: "Label");

            migrationBuilder.AddColumn<string>(
                name: "LabelActionsJson",
                table: "mail_check_settings",
                type: "text",
                nullable: false,
                defaultValue: "{\"rejected\":\"trash\",\"applied\":\"keep\",\"schedule\":\"pin\",\"scheduled\":\"pin\",\"assessment\":\"pin\",\"availability\":\"pin\",\"success\":\"pin\",\"other\":\"keep\"}");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LabelActionsJson",
                table: "mail_check_settings");

            migrationBuilder.RenameColumn(
                name: "Label",
                table: "mail_check_processed_messages",
                newName: "Decision");
        }
    }
}
