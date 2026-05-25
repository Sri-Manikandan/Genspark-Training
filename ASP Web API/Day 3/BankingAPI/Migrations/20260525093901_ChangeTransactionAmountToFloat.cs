using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingAPI.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTransactionAmountToFloat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Transaction",
                newName: "Transactions");

            migrationBuilder.RenameIndex(
                name: "IX_Transaction_to_account_number",
                table: "Transactions",
                newName: "IX_Transactions_to_account_number");

            migrationBuilder.RenameIndex(
                name: "IX_Transaction_from_account_number",
                table: "Transactions",
                newName: "IX_Transactions_from_account_number");

            migrationBuilder.AlterColumn<float>(
                name: "amount",
                table: "Transactions",
                type: "real",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Transactions",
                newName: "Transaction");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_to_account_number",
                table: "Transaction",
                newName: "IX_Transaction_to_account_number");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_from_account_number",
                table: "Transaction",
                newName: "IX_Transaction_from_account_number");

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "Transaction",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");
        }
    }
}
