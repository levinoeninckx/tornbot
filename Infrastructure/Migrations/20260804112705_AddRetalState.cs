using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TornBot.Bot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRetalState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "State",
                table: "TrackedAttacks",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "State",
                table: "TrackedAttacks");
        }
    }
}
