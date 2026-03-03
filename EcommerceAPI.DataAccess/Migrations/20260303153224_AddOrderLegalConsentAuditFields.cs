using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderLegalConsentAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcceptedFromIp",
                table: "TBL_Orders",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DistanceSalesContractAcceptedAt",
                table: "TBL_Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreliminaryInfoAcceptedAt",
                table: "TBL_Orders",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedFromIp",
                table: "TBL_Orders");

            migrationBuilder.DropColumn(
                name: "DistanceSalesContractAcceptedAt",
                table: "TBL_Orders");

            migrationBuilder.DropColumn(
                name: "PreliminaryInfoAcceptedAt",
                table: "TBL_Orders");
        }
    }
}
