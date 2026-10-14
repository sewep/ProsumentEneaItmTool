using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProsumentEneaItmTool.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RcemPrices",
                columns: table => new
                {
                    Month = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PricePlnMwh = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RcemPrices", x => x.Month);
                });

            migrationBuilder.CreateTable(
                name: "RcePrices",
                columns: table => new
                {
                    Hour = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PricePlnMwh = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RcePrices", x => x.Hour);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RcemPrices");

            migrationBuilder.DropTable(
                name: "RcePrices");
        }
    }
}
