using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TornBot.Bot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkOcToFaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FactionId",
                table: "OrganizedCrimes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizedCrimes_FactionId",
                table: "OrganizedCrimes",
                column: "FactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizedCrimes_Factions_FactionId",
                table: "OrganizedCrimes",
                column: "FactionId",
                principalTable: "Factions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganizedCrimes_Factions_FactionId",
                table: "OrganizedCrimes");

            migrationBuilder.DropIndex(
                name: "IX_OrganizedCrimes_FactionId",
                table: "OrganizedCrimes");

            migrationBuilder.DropColumn(
                name: "FactionId",
                table: "OrganizedCrimes");
        }
    }
}
