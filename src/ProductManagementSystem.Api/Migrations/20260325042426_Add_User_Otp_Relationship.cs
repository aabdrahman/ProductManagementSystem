using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductManagementSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class Add_User_Otp_Relationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Users_UserEmailAddress",
                table: "Users",
                column: "UserEmailAddress");

            migrationBuilder.AddForeignKey(
                name: "FK_UserOtpVerifications_Users_UserEmail",
                table: "UserOtpVerifications",
                column: "UserEmail",
                principalTable: "Users",
                principalColumn: "UserEmailAddress",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserOtpVerifications_Users_UserEmail",
                table: "UserOtpVerifications");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Users_UserEmailAddress",
                table: "Users");
        }
    }
}
