using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ExchangeBroker.Data.Migrations
{
    public partial class Exchange : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<sbyte>(
                name: "Status",
                table: "Payment",
                nullable: false,
                oldClrType: typeof(int));

            migrationBuilder.CreateTable(
                name: "Exchange",
                columns: table => new
                {
                    ExchangeId = table.Column<string>(type: "varchar(128)", nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    Status = table.Column<sbyte>(nullable: false),
                    SellAmount = table.Column<decimal>(nullable: false),
                    SellCurrency = table.Column<string>(nullable: false),
                    BuyAmount = table.Column<decimal>(nullable: false),
                    BuyCurrency = table.Column<string>(nullable: false),
                    SellToUsdRate = table.Column<decimal>(nullable: false),
                    GraftToUsdRate = table.Column<decimal>(nullable: false),
                    ExchangeBrokerFee = table.Column<decimal>(nullable: false),
                    BuyerAmount = table.Column<decimal>(nullable: false),
                    BuyerWallet = table.Column<string>(nullable: false),
                    PayWalletAddress = table.Column<string>(nullable: false),
                    PayAddressIndex = table.Column<int>(nullable: false),
                    ReceivedConfirmations = table.Column<int>(nullable: false),
                    ReceivedAmount = table.Column<decimal>(nullable: false),
                    BuyerTransactionStatus = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exchange", x => x.ExchangeId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Exchange");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Payment",
                nullable: false,
                oldClrType: typeof(sbyte));
        }
    }
}
