using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EcommerceAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddLoyaltyTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LoyaltyDiscountAmount",
                table: "TBL_Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "LoyaltyPointsEarned",
                table: "TBL_Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LoyaltyPointsUsed",
                table: "TBL_Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "SubtotalAmount",
                table: "TBL_Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""
                UPDATE "TBL_Orders"
                SET "SubtotalAmount" = "TotalAmount"
                WHERE "SubtotalAmount" = 0;
                """);

            migrationBuilder.CreateTable(
                name: "TBL_LoyaltyTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    BalanceAfter = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBL_LoyaltyTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TBL_LoyaltyTransactions_TBL_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "TBL_Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TBL_LoyaltyTransactions_TBL_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "TBL_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TBL_LoyaltyTransactions_CreatedAt",
                table: "TBL_LoyaltyTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_LoyaltyTransactions_OrderId",
                table: "TBL_LoyaltyTransactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_LoyaltyTransactions_OrderId_Type",
                table: "TBL_LoyaltyTransactions",
                columns: new[] { "OrderId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_TBL_LoyaltyTransactions_UserId",
                table: "TBL_LoyaltyTransactions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TBL_LoyaltyTransactions");

            migrationBuilder.DropColumn(
                name: "LoyaltyDiscountAmount",
                table: "TBL_Orders");

            migrationBuilder.DropColumn(
                name: "LoyaltyPointsEarned",
                table: "TBL_Orders");

            migrationBuilder.DropColumn(
                name: "LoyaltyPointsUsed",
                table: "TBL_Orders");

            migrationBuilder.DropColumn(
                name: "SubtotalAmount",
                table: "TBL_Orders");
        }
    }
}
