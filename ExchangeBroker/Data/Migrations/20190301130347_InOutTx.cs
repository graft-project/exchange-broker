using Microsoft.EntityFrameworkCore.Migrations;

namespace ExchangeBroker.Data.Migrations
{
    public partial class InOutTx : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuyerTransactionStatus",
                table: "Exchange");

            migrationBuilder.RenameColumn(
                name: "BuyerTransactionStatusDescription",
                table: "Exchange",
                newName: "OutTxStatusDescription");

            migrationBuilder.RenameColumn(
                name: "BuyerTransactionId",
                table: "Exchange",
                newName: "OutTxId");

            migrationBuilder.RenameColumn(
                name: "BlockNumber",
                table: "Exchange",
                newName: "OutBlockNumber");

            migrationBuilder.AddColumn<int>(
                name: "InBlockNumber",
                table: "Exchange",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "InTxId",
                table: "Exchange",
                nullable: true);

            migrationBuilder.AddColumn<sbyte>(
                name: "InTxStatus",
                table: "Exchange",
                nullable: false,
                defaultValue: (sbyte)0);

            migrationBuilder.AddColumn<string>(
                name: "InTxStatusDescription",
                table: "Exchange",
                nullable: true);

            migrationBuilder.AddColumn<sbyte>(
                name: "OutTxStatus",
                table: "Exchange",
                nullable: false,
                defaultValue: (sbyte)0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InBlockNumber",
                table: "Exchange");

            migrationBuilder.DropColumn(
                name: "InTxId",
                table: "Exchange");

            migrationBuilder.DropColumn(
                name: "InTxStatus",
                table: "Exchange");

            migrationBuilder.DropColumn(
                name: "InTxStatusDescription",
                table: "Exchange");

            migrationBuilder.DropColumn(
                name: "OutTxStatus",
                table: "Exchange");

            migrationBuilder.RenameColumn(
                name: "OutTxStatusDescription",
                table: "Exchange",
                newName: "BuyerTransactionStatusDescription");

            migrationBuilder.RenameColumn(
                name: "OutTxId",
                table: "Exchange",
                newName: "BuyerTransactionId");

            migrationBuilder.RenameColumn(
                name: "OutBlockNumber",
                table: "Exchange",
                newName: "BlockNumber");

            migrationBuilder.AddColumn<sbyte>(
                name: "BuyerTransactionStatus",
                table: "Exchange",
                nullable: false,
                defaultValue: (sbyte)0);
        }
    }
}
