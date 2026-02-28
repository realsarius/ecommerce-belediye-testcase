using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddWishlistSharing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "TBL_Wishlists",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ShareToken",
                table: "TBL_Wishlists",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TBL_Wishlists_ShareToken",
                table: "TBL_Wishlists",
                column: "ShareToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TBL_Wishlists_ShareToken",
                table: "TBL_Wishlists");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "TBL_Wishlists");

            migrationBuilder.DropColumn(
                name: "ShareToken",
                table: "TBL_Wishlists");
        }
    }
}
