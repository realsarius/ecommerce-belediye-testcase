using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EcommerceAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnAndRefundRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TBL_ReturnRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    RequestNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RequestedRefundAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ReviewedByUserId = table.Column<int>(type: "integer", nullable: true),
                    ReviewNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBL_ReturnRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TBL_ReturnRequests_TBL_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "TBL_Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TBL_ReturnRequests_TBL_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "TBL_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TBL_ReturnRequests_TBL_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "TBL_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TBL_RefundRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReturnRequestId = table.Column<int>(type: "integer", nullable: false),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    PaymentId = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ProviderRefundId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBL_RefundRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TBL_RefundRequests_TBL_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "TBL_Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TBL_RefundRequests_TBL_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "TBL_Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TBL_RefundRequests_TBL_ReturnRequests_ReturnRequestId",
                        column: x => x.ReturnRequestId,
                        principalTable: "TBL_ReturnRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TBL_RefundRequests_IdempotencyKey",
                table: "TBL_RefundRequests",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TBL_RefundRequests_OrderId",
                table: "TBL_RefundRequests",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_RefundRequests_PaymentId",
                table: "TBL_RefundRequests",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_RefundRequests_ReturnRequestId",
                table: "TBL_RefundRequests",
                column: "ReturnRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TBL_ReturnRequests_OrderId",
                table: "TBL_ReturnRequests",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_ReturnRequests_ReviewedByUserId",
                table: "TBL_ReturnRequests",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_ReturnRequests_Status",
                table: "TBL_ReturnRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_ReturnRequests_UserId",
                table: "TBL_ReturnRequests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TBL_RefundRequests");

            migrationBuilder.DropTable(
                name: "TBL_ReturnRequests");
        }
    }
}
