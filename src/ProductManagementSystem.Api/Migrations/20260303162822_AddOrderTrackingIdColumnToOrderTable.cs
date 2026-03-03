using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductManagementSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderTrackingIdColumnToOrderTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrderTrackingId",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderTrackingId",
                table: "Orders",
                column: "OrderTrackingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderTrackingId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OrderTrackingId",
                table: "Orders");
        }
    }
}
