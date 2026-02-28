using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddWishlistCountToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WishlistCount",
                table: "TBL_Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Populate WishlistCount from existing wishlist items
            migrationBuilder.Sql(@"
                UPDATE ""TBL_Products"" p
                SET ""WishlistCount"" = (
                    SELECT COUNT(*)
                    FROM ""TBL_WishlistItems"" wi
                    WHERE wi.""ProductId"" = p.""Id""
                )
            ");

            migrationBuilder.Sql(@"
                UPDATE ""TBL_WishlistItems"" wi
                SET ""AddedAt"" = wi.""CreatedAt"",
                    ""AddedAtPrice"" = COALESCE(p.""Price"", wi.""AddedAtPrice"")
                FROM ""TBL_Products"" p
                WHERE p.""Id"" = wi.""ProductId""
                  AND (wi.""AddedAt"" = TIMESTAMPTZ '0001-01-01 00:00:00+00' OR wi.""AddedAtPrice"" = 0)
            ");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_Products_WishlistCount",
                table: "TBL_Products",
                column: "WishlistCount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TBL_Products_WishlistCount",
                table: "TBL_Products");

            migrationBuilder.DropColumn(
                name: "WishlistCount",
                table: "TBL_Products");
        }
    }
}
