using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EcommerceAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddWishlistCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TBL_WishlistCollections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WishlistId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBL_WishlistCollections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TBL_WishlistCollections_TBL_Wishlists_WishlistId",
                        column: x => x.WishlistId,
                        principalTable: "TBL_Wishlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddColumn<int>(
                name: "CollectionId",
                table: "TBL_WishlistItems",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(@"
                INSERT INTO ""TBL_WishlistCollections"" (""WishlistId"", ""Name"", ""IsDefault"", ""CreatedAt"", ""UpdatedAt"")
                SELECT w.""Id"", 'Favorilerim', true, NOW(), NOW()
                FROM ""TBL_Wishlists"" AS w
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM ""TBL_WishlistCollections"" AS wc
                    WHERE wc.""WishlistId"" = w.""Id""
                      AND wc.""IsDefault"" = true
                );");

            migrationBuilder.Sql(@"
                UPDATE ""TBL_WishlistItems"" AS wi
                SET ""CollectionId"" = wc.""Id""
                FROM ""TBL_WishlistCollections"" AS wc
                WHERE wc.""WishlistId"" = wi.""WishlistId""
                  AND wc.""IsDefault"" = true
                  AND wi.""CollectionId"" IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "CollectionId",
                table: "TBL_WishlistItems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TBL_WishlistItems_CollectionId",
                table: "TBL_WishlistItems",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_WishlistCollections_WishlistId",
                table: "TBL_WishlistCollections",
                column: "WishlistId",
                unique: true,
                filter: "\"IsDefault\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_WishlistCollections_WishlistId_Name",
                table: "TBL_WishlistCollections",
                columns: new[] { "WishlistId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_WishlistItems_TBL_WishlistCollections_CollectionId",
                table: "TBL_WishlistItems",
                column: "CollectionId",
                principalTable: "TBL_WishlistCollections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TBL_WishlistItems_TBL_WishlistCollections_CollectionId",
                table: "TBL_WishlistItems");

            migrationBuilder.DropTable(
                name: "TBL_WishlistCollections");

            migrationBuilder.DropIndex(
                name: "IX_TBL_WishlistItems_CollectionId",
                table: "TBL_WishlistItems");

            migrationBuilder.DropColumn(
                name: "CollectionId",
                table: "TBL_WishlistItems");
        }
    }
}
