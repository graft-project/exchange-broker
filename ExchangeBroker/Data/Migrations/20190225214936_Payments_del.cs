using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ExchangeBroker.Data.Migrations
{
    public partial class Payments_del : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payment");

            migrationBuilder.AddColumn<string>(
                name: "BuyerTransactionStatusDescription",
                table: "Exchange",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuyerTransactionStatusDescription",
                table: "Exchange");

            migrationBuilder.CreateTable(
                name: "Payment",
                columns: table => new
                {
                    PaymentId = table.Column<string>(type: "varchar(128)", nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    ExchangeBrokerFee = table.Column<decimal>(nullable: false),
                    GraftToSaleRate = table.Column<decimal>(nullable: false),
                    PayAddressIndex = table.Column<int>(nullable: false),
                    PayAmount = table.Column<decimal>(nullable: false),
                    PayCurrency = table.Column<string>(nullable: false),
                    PayToSaleRate = table.Column<decimal>(nullable: false),
                    PayWalletAddress = table.Column<string>(nullable: false),
                    ProviderTransactionId = table.Column<string>(nullable: true),
                    ProviderTransactionStatus = table.Column<int>(nullable: false),
                    ProviderTransactionStatusDescription = table.Column<string>(nullable: true),
                    ReceivedAmount = table.Column<decimal>(nullable: false),
                    ReceivedConfirmations = table.Column<int>(nullable: false),
                    SaleAmount = table.Column<decimal>(nullable: false),
                    SaleCurrency = table.Column<string>(nullable: false),
                    ServiceProviderAmount = table.Column<decimal>(nullable: false),
                    ServiceProviderWallet = table.Column<string>(nullable: true),
                    Status = table.Column<sbyte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payment", x => x.PaymentId);
                });
        }
    }
}
