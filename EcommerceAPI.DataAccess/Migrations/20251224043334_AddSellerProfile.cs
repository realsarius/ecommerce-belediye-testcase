using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EcommerceAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SellerId",
                table: "TBL_Products",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TBL_SellerProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    BrandName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BrandDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBL_SellerProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TBL_SellerProfiles_TBL_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "TBL_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TBL_Products_SellerId",
                table: "TBL_Products",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_SellerProfiles_BrandName",
                table: "TBL_SellerProfiles",
                column: "BrandName");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_SellerProfiles_UserId",
                table: "TBL_SellerProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_Products_TBL_SellerProfiles_SellerId",
                table: "TBL_Products",
                column: "SellerId",
                principalTable: "TBL_SellerProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TBL_Products_TBL_SellerProfiles_SellerId",
                table: "TBL_Products");

            migrationBuilder.DropTable(
                name: "TBL_SellerProfiles");

            migrationBuilder.DropIndex(
                name: "IX_TBL_Products_SellerId",
                table: "TBL_Products");

            migrationBuilder.DropColumn(
                name: "SellerId",
                table: "TBL_Products");
        }
    }
}
