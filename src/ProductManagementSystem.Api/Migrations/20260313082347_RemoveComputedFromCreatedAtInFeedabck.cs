using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductManagementSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveComputedFromCreatedAtInFeedabck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Feedbacks",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComputedColumnSql: "GETUTCDATE()");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_CreatedAt",
                table: "Feedbacks",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_CreatedAt",
                table: "Feedbacks");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Feedbacks",
                type: "datetime2",
                nullable: false,
                computedColumnSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");
        }
    }
}
