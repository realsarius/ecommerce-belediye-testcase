using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerificationCodeFallbackFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmailVerificationCodeAttemptCount",
                table: "TBL_Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationCodeExpiry",
                table: "TBL_Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationCodeHash",
                table: "TBL_Users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationCodeLastSentAt",
                table: "TBL_Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationCodeLockedUntil",
                table: "TBL_Users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailVerificationCodeAttemptCount",
                table: "TBL_Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationCodeExpiry",
                table: "TBL_Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationCodeHash",
                table: "TBL_Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationCodeLastSentAt",
                table: "TBL_Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationCodeLockedUntil",
                table: "TBL_Users");
        }
    }
}
