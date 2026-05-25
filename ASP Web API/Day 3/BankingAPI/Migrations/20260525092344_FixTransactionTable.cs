using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingAPI.Migrations
{
    /// <inheritdoc />
    public partial class FixTransactionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_Accounts_AccountNumber",
                table: "Transaction");

            migrationBuilder.DropIndex(
                name: "IX_Transaction_AccountNumber",
                table: "Transaction");

            migrationBuilder.DropColumn(
                name: "AccountNumber",
                table: "Transaction");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountNumber",
                table: "Transaction",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_AccountNumber",
                table: "Transaction",
                column: "AccountNumber");

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_Accounts_AccountNumber",
                table: "Transaction",
                column: "AccountNumber",
                principalTable: "Accounts",
                principalColumn: "AccountNumber");
        }
    }
}
