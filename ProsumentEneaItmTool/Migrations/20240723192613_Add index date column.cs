using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProsumentEneaItmTool.Migrations
{
    /// <inheritdoc />
    public partial class Addindexdatecolumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IDX_DateRecord",
                table: "ImportedRecords",
                column: "Date",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IDX_DateRecord",
                table: "ImportedRecords");
        }
    }
}
