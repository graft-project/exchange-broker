using Microsoft.EntityFrameworkCore.Migrations;

namespace ExchangeBroker.Data.Migrations
{
    public partial class WalletAddress : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MerchantWallet",
                table: "Payment",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceProviderWallet",
                table: "Payment",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MerchantWallet",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "ServiceProviderWallet",
                table: "Payment");
        }
    }
}
