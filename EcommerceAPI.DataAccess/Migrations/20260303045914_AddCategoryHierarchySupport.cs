using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryHierarchySupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentCategoryId",
                table: "TBL_Categories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "TBL_Categories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TBL_Categories_ParentCategoryId",
                table: "TBL_Categories",
                column: "ParentCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_Categories_TBL_Categories_ParentCategoryId",
                table: "TBL_Categories",
                column: "ParentCategoryId",
                principalTable: "TBL_Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TBL_Categories_TBL_Categories_ParentCategoryId",
                table: "TBL_Categories");

            migrationBuilder.DropIndex(
                name: "IX_TBL_Categories_ParentCategoryId",
                table: "TBL_Categories");

            migrationBuilder.DropColumn(
                name: "ParentCategoryId",
                table: "TBL_Categories");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "TBL_Categories");
        }
    }
}
