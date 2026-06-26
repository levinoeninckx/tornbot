using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TornBot.Bot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHasFactionAndHasCompanyFieldsToApiKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasCompanyAccess",
                table: "ApiKeys",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasFactionAccess",
                table: "ApiKeys",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasCompanyAccess",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "HasFactionAccess",
                table: "ApiKeys");
        }
    }
}
