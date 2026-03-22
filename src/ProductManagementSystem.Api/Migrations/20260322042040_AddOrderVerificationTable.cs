using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductManagementSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderVerificationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderStatus",
                table: "Orders");

            migrationBuilder.CreateTable(
                name: "UserOrderVerificationTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VerificationToken = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    OrderId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOrderVerificationTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserOrderVerificationTokens_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderStatus",
                table: "Orders",
                sql: "[OrderStatus] IN ('Pending', 'Processing', 'Confirmed', 'Delivered', 'Cancelled')");

            migrationBuilder.CreateIndex(
                name: "IX_UserOrderVerificationTokens_Id",
                table: "UserOrderVerificationTokens",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_UserOrderVerificationTokens_OrderId",
                table: "UserOrderVerificationTokens",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOrderVerificationTokens_VerificationToken",
                table: "UserOrderVerificationTokens",
                column: "VerificationToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserOrderVerificationTokens");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderStatus",
                table: "Orders");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderStatus",
                table: "Orders",
                sql: "[OrderStatus] IN ('Pending', 'Processing', 'Delivered', 'Cancelled')");
        }
    }
}
