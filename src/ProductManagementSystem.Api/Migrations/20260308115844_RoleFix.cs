using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductManagementSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class RoleFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Role_Users_CreatedByUserId",
                table: "Role");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Role_RoleId",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Role",
                table: "Role");

            migrationBuilder.RenameTable(
                name: "Role",
                newName: "Roles");

            migrationBuilder.RenameIndex(
                name: "IX_Role_UserId",
                table: "Roles",
                newName: "IX_Roles_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Role_NormalizedName",
                table: "Roles",
                newName: "IX_Roles_NormalizedName");

            migrationBuilder.RenameIndex(
                name: "IX_Role_IsActive",
                table: "Roles",
                newName: "IX_Roles_IsActive");

            migrationBuilder.RenameIndex(
                name: "IX_Role_CreatedByUserId",
                table: "Roles",
                newName: "IX_Roles_CreatedByUserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Roles",
                table: "Roles",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Roles_Users_CreatedByUserId",
                table: "Roles",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Roles_Users_CreatedByUserId",
                table: "Roles");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Roles",
                table: "Roles");

            migrationBuilder.RenameTable(
                name: "Roles",
                newName: "Role");

            migrationBuilder.RenameIndex(
                name: "IX_Roles_UserId",
                table: "Role",
                newName: "IX_Role_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Roles_NormalizedName",
                table: "Role",
                newName: "IX_Role_NormalizedName");

            migrationBuilder.RenameIndex(
                name: "IX_Roles_IsActive",
                table: "Role",
                newName: "IX_Role_IsActive");

            migrationBuilder.RenameIndex(
                name: "IX_Roles_CreatedByUserId",
                table: "Role",
                newName: "IX_Role_CreatedByUserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Role",
                table: "Role",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Role_Users_CreatedByUserId",
                table: "Role",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Role_RoleId",
                table: "Users",
                column: "RoleId",
                principalTable: "Role",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
