using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerReplyToProductReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SellerRepliedAt",
                table: "ProductReviews",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SellerRepliedByUserId",
                table: "ProductReviews",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerReply",
                table: "ProductReviews",
                type: "character varying(1500)",
                maxLength: 1500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SellerRepliedAt",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "SellerRepliedByUserId",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "SellerReply",
                table: "ProductReviews");
        }
    }
}
