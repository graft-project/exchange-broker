using Microsoft.EntityFrameworkCore.Migrations;

namespace ExchangeBroker.Data.Migrations
{
    public partial class Remove_AppParams : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppParam");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppParam",
                columns: table => new
                {
                    ID = table.Column<string>(nullable: false),
                    param_name = table.Column<string>(type: "varchar(100)", nullable: false),
                    param_value = table.Column<string>(type: "varchar(1000)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppParam", x => x.ID);
                });
        }
    }
}
