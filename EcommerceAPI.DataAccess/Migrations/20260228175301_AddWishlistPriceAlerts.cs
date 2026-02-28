using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EcommerceAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddWishlistPriceAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TBL_PriceAlerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    TargetPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastKnownPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LastTriggeredPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    LastNotifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBL_PriceAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TBL_PriceAlerts_TBL_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "TBL_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TBL_PriceAlerts_TBL_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "TBL_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TBL_PriceAlerts_IsActive_ProductId",
                table: "TBL_PriceAlerts",
                columns: new[] { "IsActive", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_TBL_PriceAlerts_ProductId",
                table: "TBL_PriceAlerts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_PriceAlerts_UserId_ProductId",
                table: "TBL_PriceAlerts",
                columns: new[] { "UserId", "ProductId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TBL_PriceAlerts");
        }
    }
}
