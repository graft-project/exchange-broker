using Microsoft.EntityFrameworkCore.Migrations;

namespace ExchangeBroker.Data.Migrations
{
    public partial class ServiceProviderAmount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MerchantAmount",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "MerchantTransactionId",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "MerchantTransactionStatus",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "MerchantWallet",
                table: "Payment");

            migrationBuilder.RenameColumn(
                name: "ServiceProviderFee",
                table: "Payment",
                newName: "ServiceProviderAmount");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ServiceProviderAmount",
                table: "Payment",
                newName: "ServiceProviderFee");

            migrationBuilder.AddColumn<decimal>(
                name: "MerchantAmount",
                table: "Payment",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MerchantTransactionId",
                table: "Payment",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MerchantTransactionStatus",
                table: "Payment",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MerchantWallet",
                table: "Payment",
                nullable: true);
        }
    }
}
