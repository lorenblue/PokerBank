using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerBank.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkGamesToEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PokerEventId",
                table: "Games",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Games_PokerEventId",
                table: "Games",
                column: "PokerEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_PokerGroupId_PokerEventId",
                table: "Games",
                columns: new[] { "PokerGroupId", "PokerEventId" },
                unique: true,
                filter: "\"PokerEventId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Events_PokerEventId",
                table: "Games",
                column: "PokerEventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_Events_PokerEventId",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_PokerEventId",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_PokerGroupId_PokerEventId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "PokerEventId",
                table: "Games");
        }
    }
}
