using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerBank.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PokerGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailAddress = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AcceptedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerInvitations_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerInvitations_PokerGroups_PokerGroupId",
                        column: x => x.PokerGroupId,
                        principalTable: "PokerGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerInvitations_PlayerId",
                table: "PlayerInvitations",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerInvitations_PokerGroupId_PlayerId",
                table: "PlayerInvitations",
                columns: new[] { "PokerGroupId", "PlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerInvitations_TokenHash",
                table: "PlayerInvitations",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerInvitations");
        }
    }
}
