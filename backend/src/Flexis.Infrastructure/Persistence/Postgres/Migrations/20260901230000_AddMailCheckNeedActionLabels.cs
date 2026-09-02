using System;
using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flexis.Infrastructure.Persistence.Postgres.Migrations
{
    [DbContext(typeof(FlexisDbContext))]
    [Migration("20260901230000_AddMailCheckNeedActionLabels")]
    public partial class AddMailCheckNeedActionLabels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NeedActionLabelsJson",
                table: "mail_check_settings",
                type: "text",
                nullable: false,
                defaultValue: "[\"schedule\",\"assessment\",\"availability\"]");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NeedActionLabelsJson",
                table: "mail_check_settings");
        }
    }
}
