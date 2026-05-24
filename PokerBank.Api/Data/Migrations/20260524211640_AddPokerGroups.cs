using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerBank.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPokerGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var defaultPokerGroupId = new Guid("11111111-1111-1111-1111-111111111111");

            migrationBuilder.CreateTable(
                name: "PokerGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PokerGroups", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "PokerGroups",
                columns: ["Id", "Name", "IsActive"],
                values: [defaultPokerGroupId, "Default Group", true]);

            migrationBuilder.AddColumn<Guid>(
                name: "PokerGroupId",
                table: "Players",
                type: "uuid",
                nullable: false,
                defaultValue: defaultPokerGroupId);

            migrationBuilder.AddColumn<Guid>(
                name: "PokerGroupId",
                table: "Payments",
                type: "uuid",
                nullable: false,
                defaultValue: defaultPokerGroupId);

            migrationBuilder.AddColumn<Guid>(
                name: "PokerGroupId",
                table: "Games",
                type: "uuid",
                nullable: false,
                defaultValue: defaultPokerGroupId);

            migrationBuilder.CreateIndex(
                name: "IX_Players_PokerGroupId",
                table: "Players",
                column: "PokerGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PokerGroupId",
                table: "Payments",
                column: "PokerGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_PokerGroupId",
                table: "Games",
                column: "PokerGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_PokerGroups_PokerGroupId",
                table: "Games",
                column: "PokerGroupId",
                principalTable: "PokerGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_PokerGroups_PokerGroupId",
                table: "Payments",
                column: "PokerGroupId",
                principalTable: "PokerGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Players_PokerGroups_PokerGroupId",
                table: "Players",
                column: "PokerGroupId",
                principalTable: "PokerGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_PokerGroups_PokerGroupId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_PokerGroups_PokerGroupId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Players_PokerGroups_PokerGroupId",
                table: "Players");

            migrationBuilder.DropTable(
                name: "PokerGroups");

            migrationBuilder.DropIndex(
                name: "IX_Players_PokerGroupId",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PokerGroupId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Games_PokerGroupId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "PokerGroupId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "PokerGroupId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PokerGroupId",
                table: "Games");
        }
    }
}
