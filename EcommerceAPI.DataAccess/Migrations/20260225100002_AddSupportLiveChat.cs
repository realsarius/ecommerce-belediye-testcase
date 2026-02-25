using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EcommerceAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportLiveChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TBL_SupportConversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Subject = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    CustomerUserId = table.Column<int>(type: "integer", nullable: false),
                    SupportUserId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LastMessageAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBL_SupportConversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TBL_SupportConversations_TBL_Users_CustomerUserId",
                        column: x => x.CustomerUserId,
                        principalTable: "TBL_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TBL_SupportConversations_TBL_Users_SupportUserId",
                        column: x => x.SupportUserId,
                        principalTable: "TBL_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TBL_SupportMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConversationId = table.Column<int>(type: "integer", nullable: false),
                    SenderUserId = table.Column<int>(type: "integer", nullable: false),
                    SenderRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    IsSystemMessage = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBL_SupportMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TBL_SupportMessages_TBL_SupportConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "TBL_SupportConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TBL_SupportMessages_TBL_Users_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "TBL_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TBL_SupportConversations_CustomerUserId",
                table: "TBL_SupportConversations",
                column: "CustomerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_SupportConversations_LastMessageAt",
                table: "TBL_SupportConversations",
                column: "LastMessageAt");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_SupportConversations_Status",
                table: "TBL_SupportConversations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_SupportConversations_SupportUserId",
                table: "TBL_SupportConversations",
                column: "SupportUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_SupportMessages_ConversationId",
                table: "TBL_SupportMessages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_SupportMessages_CreatedAt",
                table: "TBL_SupportMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_SupportMessages_SenderUserId",
                table: "TBL_SupportMessages",
                column: "SenderUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TBL_SupportMessages");

            migrationBuilder.DropTable(
                name: "TBL_SupportConversations");
        }
    }
}
