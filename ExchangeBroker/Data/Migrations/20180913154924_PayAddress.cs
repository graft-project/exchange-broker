using Microsoft.EntityFrameworkCore.Migrations;

namespace ExchangeBroker.Data.Migrations
{
    public partial class PayAddress : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PayAddress",
                table: "Payment",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PayAddressIndex",
                table: "Payment",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayAddress",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "PayAddressIndex",
                table: "Payment");
        }
    }
}
