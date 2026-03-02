using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EcommerceAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddReferralSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AppliedReferralCodeId",
                table: "TBL_Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReferralRewardedOrderId",
                table: "TBL_Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReferredByUserId",
                table: "TBL_Users",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TBL_ReferralCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBL_ReferralCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TBL_ReferralCodes_TBL_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "TBL_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TBL_ReferralTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReferralCodeId = table.Column<int>(type: "integer", nullable: false),
                    ReferrerUserId = table.Column<int>(type: "integer", nullable: false),
                    ReferredUserId = table.Column<int>(type: "integer", nullable: false),
                    BeneficiaryUserId = table.Column<int>(type: "integer", nullable: true),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBL_ReferralTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TBL_ReferralTransactions_TBL_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "TBL_Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TBL_ReferralTransactions_TBL_ReferralCodes_ReferralCodeId",
                        column: x => x.ReferralCodeId,
                        principalTable: "TBL_ReferralCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TBL_ReferralTransactions_TBL_Users_BeneficiaryUserId",
                        column: x => x.BeneficiaryUserId,
                        principalTable: "TBL_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TBL_ReferralTransactions_TBL_Users_ReferredUserId",
                        column: x => x.ReferredUserId,
                        principalTable: "TBL_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TBL_ReferralTransactions_TBL_Users_ReferrerUserId",
                        column: x => x.ReferrerUserId,
                        principalTable: "TBL_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TBL_Users_AppliedReferralCodeId",
                table: "TBL_Users",
                column: "AppliedReferralCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_Users_ReferralRewardedOrderId",
                table: "TBL_Users",
                column: "ReferralRewardedOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_Users_ReferredByUserId",
                table: "TBL_Users",
                column: "ReferredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_ReferralCodes_Code",
                table: "TBL_ReferralCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TBL_ReferralCodes_UserId",
                table: "TBL_ReferralCodes",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TBL_ReferralTransactions_BeneficiaryUserId",
                table: "TBL_ReferralTransactions",
                column: "BeneficiaryUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_ReferralTransactions_CreatedAt",
                table: "TBL_ReferralTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_ReferralTransactions_OrderId",
                table: "TBL_ReferralTransactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_ReferralTransactions_ReferralCodeId",
                table: "TBL_ReferralTransactions",
                column: "ReferralCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_ReferralTransactions_ReferredUserId",
                table: "TBL_ReferralTransactions",
                column: "ReferredUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_ReferralTransactions_ReferrerUserId",
                table: "TBL_ReferralTransactions",
                column: "ReferrerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_ReferralTransactions_Type",
                table: "TBL_ReferralTransactions",
                column: "Type");

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_Users_TBL_Orders_ReferralRewardedOrderId",
                table: "TBL_Users",
                column: "ReferralRewardedOrderId",
                principalTable: "TBL_Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_Users_TBL_ReferralCodes_AppliedReferralCodeId",
                table: "TBL_Users",
                column: "AppliedReferralCodeId",
                principalTable: "TBL_ReferralCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_Users_TBL_Users_ReferredByUserId",
                table: "TBL_Users",
                column: "ReferredByUserId",
                principalTable: "TBL_Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TBL_Users_TBL_Orders_ReferralRewardedOrderId",
                table: "TBL_Users");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_Users_TBL_ReferralCodes_AppliedReferralCodeId",
                table: "TBL_Users");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_Users_TBL_Users_ReferredByUserId",
                table: "TBL_Users");

            migrationBuilder.DropTable(
                name: "TBL_ReferralTransactions");

            migrationBuilder.DropTable(
                name: "TBL_ReferralCodes");

            migrationBuilder.DropIndex(
                name: "IX_TBL_Users_AppliedReferralCodeId",
                table: "TBL_Users");

            migrationBuilder.DropIndex(
                name: "IX_TBL_Users_ReferralRewardedOrderId",
                table: "TBL_Users");

            migrationBuilder.DropIndex(
                name: "IX_TBL_Users_ReferredByUserId",
                table: "TBL_Users");

            migrationBuilder.DropColumn(
                name: "AppliedReferralCodeId",
                table: "TBL_Users");

            migrationBuilder.DropColumn(
                name: "ReferralRewardedOrderId",
                table: "TBL_Users");

            migrationBuilder.DropColumn(
                name: "ReferredByUserId",
                table: "TBL_Users");
        }
    }
}
