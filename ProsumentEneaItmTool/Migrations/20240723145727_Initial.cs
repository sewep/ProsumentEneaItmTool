using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProsumentEneaItmTool.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Blogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TakenVolumeBeforeBanancing = table.Column<double>(type: "REAL", nullable: false),
                    FedVolumeBeforeBanancing = table.Column<double>(type: "REAL", nullable: false),
                    TakenVolumeAfterBanancing = table.Column<double>(type: "REAL", nullable: false),
                    FedVolumeAfterBanancing = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Blogs");
        }
    }
}
