using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Transaction",
                columns: table => new
                {
                    Transaction_reference_number = table.Column<string>(type: "text", nullable: false),
                    from_account_number = table.Column<string>(type: "text", nullable: true),
                    to_account_number = table.Column<string>(type: "text", nullable: true),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    transaction_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    transaction_status = table.Column<string>(type: "text", nullable: false),
                    AccountNumber = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transaction_reference_number", x => x.Transaction_reference_number);
                    table.ForeignKey(
                        name: "FK_Transaction_Accounts_AccountNumber",
                        column: x => x.AccountNumber,
                        principalTable: "Accounts",
                        principalColumn: "AccountNumber");
                    table.ForeignKey(
                        name: "FK_Transaction_FromAccount",
                        column: x => x.from_account_number,
                        principalTable: "Accounts",
                        principalColumn: "AccountNumber",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transaction_ToAccount",
                        column: x => x.to_account_number,
                        principalTable: "Accounts",
                        principalColumn: "AccountNumber",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_AccountNumber",
                table: "Transaction",
                column: "AccountNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_from_account_number",
                table: "Transaction",
                column: "from_account_number");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_to_account_number",
                table: "Transaction",
                column: "to_account_number");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transaction");
        }
    }
}
