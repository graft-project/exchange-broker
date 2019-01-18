using Microsoft.EntityFrameworkCore.Migrations;

namespace ExchangeBroker.Data.Migrations
{
    public partial class Transactionsave : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MerchantTransactionId",
                table: "Payment",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderTransactionId",
                table: "Payment",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuyerTransactionId",
                table: "Exchange",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TransactionRequests",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Address = table.Column<string>(nullable: true),
                    Amount = table.Column<ulong>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    TxId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionRequests", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransactionRequests");

            migrationBuilder.DropColumn(
                name: "MerchantTransactionId",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "ProviderTransactionId",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "BuyerTransactionId",
                table: "Exchange");
        }
    }
}
