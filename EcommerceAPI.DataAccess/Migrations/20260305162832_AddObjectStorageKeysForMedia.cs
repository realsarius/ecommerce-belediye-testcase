using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddObjectStorageKeysForMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BannerImageObjectKey",
                table: "TBL_SellerProfiles",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoObjectKey",
                table: "TBL_SellerProfiles",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObjectKey",
                table: "TBL_ProductImages",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageObjectKey",
                table: "TBL_Categories",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "TBL_Categories",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannerImageObjectKey",
                table: "TBL_SellerProfiles");

            migrationBuilder.DropColumn(
                name: "LogoObjectKey",
                table: "TBL_SellerProfiles");

            migrationBuilder.DropColumn(
                name: "ObjectKey",
                table: "TBL_ProductImages");

            migrationBuilder.DropColumn(
                name: "ImageObjectKey",
                table: "TBL_Categories");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "TBL_Categories");
        }
    }
}
