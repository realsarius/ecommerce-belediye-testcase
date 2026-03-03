using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerProfileContactChannels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BannerImageUrl",
                table: "TBL_SellerProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "TBL_SellerProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "TBL_SellerProfiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacebookUrl",
                table: "TBL_SellerProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstagramUrl",
                table: "TBL_SellerProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebsiteUrl",
                table: "TBL_SellerProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "XUrl",
                table: "TBL_SellerProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannerImageUrl",
                table: "TBL_SellerProfiles");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "TBL_SellerProfiles");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "TBL_SellerProfiles");

            migrationBuilder.DropColumn(
                name: "FacebookUrl",
                table: "TBL_SellerProfiles");

            migrationBuilder.DropColumn(
                name: "InstagramUrl",
                table: "TBL_SellerProfiles");

            migrationBuilder.DropColumn(
                name: "WebsiteUrl",
                table: "TBL_SellerProfiles");

            migrationBuilder.DropColumn(
                name: "XUrl",
                table: "TBL_SellerProfiles");
        }
    }
}
