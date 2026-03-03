using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentProviderAndCardTokenMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Provider",
                table: "TBL_Payments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IyzicoCardToken",
                table: "TBL_CreditCards",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IyzicoUserKey",
                table: "TBL_CreditCards",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayTrToken",
                table: "TBL_CreditCards",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                table: "TBL_CreditCards",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripePaymentMethodId",
                table: "TBL_CreditCards",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TokenProvider",
                table: "TBL_CreditCards",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Provider",
                table: "TBL_Payments");

            migrationBuilder.DropColumn(
                name: "IyzicoCardToken",
                table: "TBL_CreditCards");

            migrationBuilder.DropColumn(
                name: "IyzicoUserKey",
                table: "TBL_CreditCards");

            migrationBuilder.DropColumn(
                name: "PayTrToken",
                table: "TBL_CreditCards");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "TBL_CreditCards");

            migrationBuilder.DropColumn(
                name: "StripePaymentMethodId",
                table: "TBL_CreditCards");

            migrationBuilder.DropColumn(
                name: "TokenProvider",
                table: "TBL_CreditCards");
        }
    }
}
