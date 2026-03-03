using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminSellerManagementFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationReviewNote",
                table: "TBL_SellerProfiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApplicationReviewedAt",
                table: "TBL_SellerProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionRateOverride",
                table: "TBL_SellerProfiles",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicationReviewNote",
                table: "TBL_SellerProfiles");

            migrationBuilder.DropColumn(
                name: "ApplicationReviewedAt",
                table: "TBL_SellerProfiles");

            migrationBuilder.DropColumn(
                name: "CommissionRateOverride",
                table: "TBL_SellerProfiles");
        }
    }
}
