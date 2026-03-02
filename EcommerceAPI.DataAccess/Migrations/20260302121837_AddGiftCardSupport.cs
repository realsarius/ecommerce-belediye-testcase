using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EcommerceAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddGiftCardSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "GiftCardAmount",
                table: "TBL_Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "GiftCardCode",
                table: "TBL_Orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GiftCardId",
                table: "TBL_Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TBL_GiftCards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    InitialBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AssignedUserId = table.Column<int>(type: "integer", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBL_GiftCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TBL_GiftCards_TBL_Users_AssignedUserId",
                        column: x => x.AssignedUserId,
                        principalTable: "TBL_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TBL_GiftCardTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GiftCardId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBL_GiftCardTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TBL_GiftCardTransactions_TBL_GiftCards_GiftCardId",
                        column: x => x.GiftCardId,
                        principalTable: "TBL_GiftCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TBL_GiftCardTransactions_TBL_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "TBL_Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TBL_GiftCardTransactions_TBL_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "TBL_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TBL_Orders_GiftCardId",
                table: "TBL_Orders",
                column: "GiftCardId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_GiftCards_AssignedUserId",
                table: "TBL_GiftCards",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_GiftCards_Code",
                table: "TBL_GiftCards",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TBL_GiftCards_ExpiresAt",
                table: "TBL_GiftCards",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_GiftCards_IsActive",
                table: "TBL_GiftCards",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_GiftCardTransactions_CreatedAt",
                table: "TBL_GiftCardTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_GiftCardTransactions_GiftCardId",
                table: "TBL_GiftCardTransactions",
                column: "GiftCardId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_GiftCardTransactions_OrderId",
                table: "TBL_GiftCardTransactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_GiftCardTransactions_UserId",
                table: "TBL_GiftCardTransactions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_Orders_TBL_GiftCards_GiftCardId",
                table: "TBL_Orders",
                column: "GiftCardId",
                principalTable: "TBL_GiftCards",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TBL_Orders_TBL_GiftCards_GiftCardId",
                table: "TBL_Orders");

            migrationBuilder.DropTable(
                name: "TBL_GiftCardTransactions");

            migrationBuilder.DropTable(
                name: "TBL_GiftCards");

            migrationBuilder.DropIndex(
                name: "IX_TBL_Orders_GiftCardId",
                table: "TBL_Orders");

            migrationBuilder.DropColumn(
                name: "GiftCardAmount",
                table: "TBL_Orders");

            migrationBuilder.DropColumn(
                name: "GiftCardCode",
                table: "TBL_Orders");

            migrationBuilder.DropColumn(
                name: "GiftCardId",
                table: "TBL_Orders");
        }
    }
}
