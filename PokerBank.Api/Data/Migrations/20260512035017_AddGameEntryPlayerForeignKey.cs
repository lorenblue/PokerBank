using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerBank.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGameEntryPlayerForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_GameEntries_PlayerId",
                table: "GameEntries",
                column: "PlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_GameEntries_Players_PlayerId",
                table: "GameEntries",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameEntries_Players_PlayerId",
                table: "GameEntries");

            migrationBuilder.DropIndex(
                name: "IX_GameEntries_PlayerId",
                table: "GameEntries");
        }
    }
}
