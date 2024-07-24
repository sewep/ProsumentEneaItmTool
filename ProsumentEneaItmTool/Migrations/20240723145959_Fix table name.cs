using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProsumentEneaItmTool.Migrations
{
    /// <inheritdoc />
    public partial class Fixtablename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Blogs",
                table: "Blogs");

            migrationBuilder.RenameTable(
                name: "Blogs",
                newName: "ImportedRecords");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ImportedRecords",
                table: "ImportedRecords",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ImportedRecords",
                table: "ImportedRecords");

            migrationBuilder.RenameTable(
                name: "ImportedRecords",
                newName: "Blogs");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Blogs",
                table: "Blogs",
                column: "Id");
        }
    }
}
