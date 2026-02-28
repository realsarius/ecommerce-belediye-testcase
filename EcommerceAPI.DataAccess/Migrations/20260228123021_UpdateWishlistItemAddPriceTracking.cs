using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateWishlistItemAddPriceTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TBL_WishlistItems_TBL_Products_ProductId",
                table: "TBL_WishlistItems");

            migrationBuilder.AddColumn<DateTime>(
                name: "AddedAt",
                table: "TBL_WishlistItems",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "AddedAtPrice",
                table: "TBL_WishlistItems",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_WishlistItems_TBL_Products_ProductId",
                table: "TBL_WishlistItems",
                column: "ProductId",
                principalTable: "TBL_Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TBL_WishlistItems_TBL_Products_ProductId",
                table: "TBL_WishlistItems");

            migrationBuilder.DropColumn(
                name: "AddedAt",
                table: "TBL_WishlistItems");

            migrationBuilder.DropColumn(
                name: "AddedAtPrice",
                table: "TBL_WishlistItems");

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_WishlistItems_TBL_Products_ProductId",
                table: "TBL_WishlistItems",
                column: "ProductId",
                principalTable: "TBL_Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
