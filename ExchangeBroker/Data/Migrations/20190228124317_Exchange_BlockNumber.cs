using Microsoft.EntityFrameworkCore.Migrations;

namespace ExchangeBroker.Data.Migrations
{
    public partial class Exchange_BlockNumber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BlockNumber",
                table: "Exchange",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlockNumber",
                table: "Exchange");
        }
    }
}
