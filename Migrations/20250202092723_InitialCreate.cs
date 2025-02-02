using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortfolioTracker.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortfolioItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MarketReturns",
                table: "MarketReturns");

            migrationBuilder.RenameTable(
                name: "MarketReturns",
                newName: "MarketReturn");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MarketReturn",
                table: "MarketReturn",
                column: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MarketReturn",
                table: "MarketReturn");

            migrationBuilder.RenameTable(
                name: "MarketReturn",
                newName: "MarketReturns");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MarketReturns",
                table: "MarketReturns",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "PortfolioItems",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChangePercentage = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Investment = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    ProfitLoss = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    ProfitLossPercentage = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Quantity = table.Column<double>(type: "double", nullable: false),
                    Stock = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioItems", x => x.ID);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
