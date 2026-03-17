using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductManagementSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class Add_OtpVerification_Table_And_IndexOnIsActiveColumnsAcrossTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserOtpVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GeneratedOTP = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOtpVerifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive",
                table: "Products",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_IsActive",
                table: "Orders",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLineItems_IsActive",
                table: "OrderLineItems",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserOtpVerifications_CreatedAt",
                table: "UserOtpVerifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserOtpVerifications_UserEmail",
                table: "UserOtpVerifications",
                column: "UserEmail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserOtpVerifications");

            migrationBuilder.DropIndex(
                name: "IX_Users_IsActive",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsActive",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Orders_IsActive",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_OrderLineItems_IsActive",
                table: "OrderLineItems");
        }
    }
}
