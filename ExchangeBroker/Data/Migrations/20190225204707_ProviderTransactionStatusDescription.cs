using Microsoft.EntityFrameworkCore.Migrations;

namespace ExchangeBroker.Data.Migrations
{
    public partial class ProviderTransactionStatusDescription : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProviderTransactionStatusDescription",
                table: "Payment",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProviderTransactionStatusDescription",
                table: "Payment");
        }
    }
}
