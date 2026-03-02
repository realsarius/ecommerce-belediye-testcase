using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialLoginFieldsToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppleSubject",
                table: "TBL_Users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleSubject",
                table: "TBL_Users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "TBL_Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_TBL_Users_AppleSubject",
                table: "TBL_Users",
                column: "AppleSubject",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TBL_Users_GoogleSubject",
                table: "TBL_Users",
                column: "GoogleSubject",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TBL_Users_AppleSubject",
                table: "TBL_Users");

            migrationBuilder.DropIndex(
                name: "IX_TBL_Users_GoogleSubject",
                table: "TBL_Users");

            migrationBuilder.DropColumn(
                name: "AppleSubject",
                table: "TBL_Users");

            migrationBuilder.DropColumn(
                name: "GoogleSubject",
                table: "TBL_Users");

            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "TBL_Users");
        }
    }
}
