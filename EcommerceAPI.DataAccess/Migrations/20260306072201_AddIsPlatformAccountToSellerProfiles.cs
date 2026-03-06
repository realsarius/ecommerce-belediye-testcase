using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPlatformAccountToSellerProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPlatformAccount",
                table: "TBL_SellerProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_TBL_SellerProfiles_IsPlatformAccount",
                table: "TBL_SellerProfiles",
                column: "IsPlatformAccount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TBL_SellerProfiles_IsPlatformAccount",
                table: "TBL_SellerProfiles");

            migrationBuilder.DropColumn(
                name: "IsPlatformAccount",
                table: "TBL_SellerProfiles");
        }
    }
}
