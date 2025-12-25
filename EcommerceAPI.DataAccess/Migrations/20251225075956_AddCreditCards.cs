using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EcommerceAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TBL_CreditCards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CardAlias = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CardHolderName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CardNumberEncrypted = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Last4Digits = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    ExpireYearEncrypted = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExpireMonthEncrypted = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CvvEncrypted = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBL_CreditCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TBL_CreditCards_TBL_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "TBL_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TBL_CreditCards_UserId",
                table: "TBL_CreditCards",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TBL_CreditCards");
        }
    }
}
