using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EcommerceAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentWebhookEventDedupe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TBL_PaymentWebhookEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    DedupeKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EventType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ProviderEventId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    PaymentId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    PaymentConversationId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    EventTime = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBL_PaymentWebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TBL_PaymentWebhookEvents_CreatedAt",
                table: "TBL_PaymentWebhookEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_PaymentWebhookEvents_PaymentConversationId",
                table: "TBL_PaymentWebhookEvents",
                column: "PaymentConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_PaymentWebhookEvents_Provider_DedupeKey",
                table: "TBL_PaymentWebhookEvents",
                columns: new[] { "Provider", "DedupeKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TBL_PaymentWebhookEvents");
        }
    }
}
