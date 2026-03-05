using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerificationPasswordResetAndEmailChangeTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailChangeToken",
                table: "TBL_Users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailChangeTokenExpiry",
                table: "TBL_Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationToken",
                table: "TBL_Users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationTokenExpiry",
                table: "TBL_Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                table: "TBL_Users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiry",
                table: "TBL_Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingEmail",
                table: "TBL_Users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailChangeToken",
                table: "TBL_Users");

            migrationBuilder.DropColumn(
                name: "EmailChangeTokenExpiry",
                table: "TBL_Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationToken",
                table: "TBL_Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationTokenExpiry",
                table: "TBL_Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                table: "TBL_Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiry",
                table: "TBL_Users");

            migrationBuilder.DropColumn(
                name: "PendingEmail",
                table: "TBL_Users");
        }
    }
}
