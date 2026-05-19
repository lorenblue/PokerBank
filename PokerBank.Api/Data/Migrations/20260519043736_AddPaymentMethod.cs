using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerBank.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Method",
                table: "Payments",
                type: "text",
                nullable: false,
                defaultValue: "ETransfer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Method",
                table: "Payments");
        }
    }
}
