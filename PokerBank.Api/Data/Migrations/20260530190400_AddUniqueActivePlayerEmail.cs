using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerBank.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueActivePlayerEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Players_PokerGroupId_EmailAddress",
                table: "Players",
                columns: new[] { "PokerGroupId", "EmailAddress" },
                unique: true,
                filter: "\"EmailAddress\" IS NOT NULL AND \"IsActive\" = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Players_PokerGroupId_EmailAddress",
                table: "Players");
        }
    }
}
