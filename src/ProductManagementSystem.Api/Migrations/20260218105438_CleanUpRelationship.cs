using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductManagementSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class CleanUpRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_ProductCategories_ProductCategoryId1",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_ProductCategoryId1",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ProductCategoryId1",
                table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductCategoryId1",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductCategoryId1",
                table: "Products",
                column: "ProductCategoryId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_ProductCategories_ProductCategoryId1",
                table: "Products",
                column: "ProductCategoryId1",
                principalTable: "ProductCategories",
                principalColumn: "Id");
        }
    }
}
