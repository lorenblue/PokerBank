using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerBank.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenamePaymentTypeToDirection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Payments",
                newName: "Direction");

            migrationBuilder.Sql("""
                UPDATE "Payments"
                SET "Direction" = CASE "Direction"
                    WHEN 'PlayerPaysBank' THEN 'MadeByPlayer'
                    WHEN 'BankPaysPlayer' THEN 'ReceivedByPlayer'
                    ELSE "Direction"
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Payments"
                SET "Direction" = CASE "Direction"
                    WHEN 'MadeByPlayer' THEN 'PlayerPaysBank'
                    WHEN 'ReceivedByPlayer' THEN 'BankPaysPlayer'
                    ELSE "Direction"
                END;
                """);

            migrationBuilder.RenameColumn(
                name: "Direction",
                table: "Payments",
                newName: "Type");
        }
    }
}
