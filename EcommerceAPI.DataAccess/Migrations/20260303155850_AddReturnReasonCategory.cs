using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnReasonCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReasonCategory",
                table: "TBL_ReturnRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TBL_ReturnRequests_ReasonCategory",
                table: "TBL_ReturnRequests",
                column: "ReasonCategory");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TBL_ReturnRequests_ReasonCategory",
                table: "TBL_ReturnRequests");

            migrationBuilder.DropColumn(
                name: "ReasonCategory",
                table: "TBL_ReturnRequests");
        }
    }
}
