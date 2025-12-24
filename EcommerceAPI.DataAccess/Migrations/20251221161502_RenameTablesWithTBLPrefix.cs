using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameTablesWithTBLPrefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_Carts_CartId",
                table: "CartItems");

            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_Products_ProductId",
                table: "CartItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Carts_Users_UserId",
                table: "Carts");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Products_ProductId",
                table: "Inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryMovements_Products_ProductId",
                table: "InventoryMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryMovements_Users_UserId",
                table: "InventoryMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Orders_OrderId",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Products_ProductId",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_UserId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Orders_OrderId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_Users_UserId",
                table: "RefreshTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_ShippingAddresses_Users_UserId",
                table: "ShippingAddresses");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ShippingAddresses",
                table: "ShippingAddresses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Roles",
                table: "Roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RefreshTokens",
                table: "RefreshTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Products",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Payments",
                table: "Payments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Orders",
                table: "Orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderItems",
                table: "OrderItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InventoryMovements",
                table: "InventoryMovements");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Inventories",
                table: "Inventories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Categories",
                table: "Categories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Carts",
                table: "Carts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CartItems",
                table: "CartItems");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "TBL_Users");

            migrationBuilder.RenameTable(
                name: "ShippingAddresses",
                newName: "TBL_ShippingAddresses");

            migrationBuilder.RenameTable(
                name: "Roles",
                newName: "TBL_Roles");

            migrationBuilder.RenameTable(
                name: "RefreshTokens",
                newName: "TBL_RefreshTokens");

            migrationBuilder.RenameTable(
                name: "Products",
                newName: "TBL_Products");

            migrationBuilder.RenameTable(
                name: "Payments",
                newName: "TBL_Payments");

            migrationBuilder.RenameTable(
                name: "Orders",
                newName: "TBL_Orders");

            migrationBuilder.RenameTable(
                name: "OrderItems",
                newName: "TBL_OrderItems");

            migrationBuilder.RenameTable(
                name: "InventoryMovements",
                newName: "TBL_InventoryMovements");

            migrationBuilder.RenameTable(
                name: "Inventories",
                newName: "TBL_Inventories");

            migrationBuilder.RenameTable(
                name: "Categories",
                newName: "TBL_Categories");

            migrationBuilder.RenameTable(
                name: "Carts",
                newName: "TBL_Carts");

            migrationBuilder.RenameTable(
                name: "CartItems",
                newName: "TBL_CartItems");

            migrationBuilder.RenameIndex(
                name: "IX_Users_RoleId",
                table: "TBL_Users",
                newName: "IX_TBL_Users_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_Users_EmailHash",
                table: "TBL_Users",
                newName: "IX_TBL_Users_EmailHash");

            migrationBuilder.RenameIndex(
                name: "IX_ShippingAddresses_UserId",
                table: "TBL_ShippingAddresses",
                newName: "IX_TBL_ShippingAddresses_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Roles_Name",
                table: "TBL_Roles",
                newName: "IX_TBL_Roles_Name");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshTokens_UserId",
                table: "TBL_RefreshTokens",
                newName: "IX_TBL_RefreshTokens_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshTokens_Token",
                table: "TBL_RefreshTokens",
                newName: "IX_TBL_RefreshTokens_Token");

            migrationBuilder.RenameIndex(
                name: "IX_Products_SKU",
                table: "TBL_Products",
                newName: "IX_TBL_Products_SKU");

            migrationBuilder.RenameIndex(
                name: "IX_Products_Price",
                table: "TBL_Products",
                newName: "IX_TBL_Products_Price");

            migrationBuilder.RenameIndex(
                name: "IX_Products_IsActive",
                table: "TBL_Products",
                newName: "IX_TBL_Products_IsActive");

            migrationBuilder.RenameIndex(
                name: "IX_Products_CategoryId",
                table: "TBL_Products",
                newName: "IX_TBL_Products_CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_Payments_OrderId",
                table: "TBL_Payments",
                newName: "IX_TBL_Payments_OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_Payments_IdempotencyKey",
                table: "TBL_Payments",
                newName: "IX_TBL_Payments_IdempotencyKey");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_UserId",
                table: "TBL_Orders",
                newName: "IX_TBL_Orders_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_Status",
                table: "TBL_Orders",
                newName: "IX_TBL_Orders_Status");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_OrderNumber",
                table: "TBL_Orders",
                newName: "IX_TBL_Orders_OrderNumber");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_CreatedAt",
                table: "TBL_Orders",
                newName: "IX_TBL_Orders_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItems_ProductId",
                table: "TBL_OrderItems",
                newName: "IX_TBL_OrderItems_ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItems_OrderId",
                table: "TBL_OrderItems",
                newName: "IX_TBL_OrderItems_OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryMovements_UserId",
                table: "TBL_InventoryMovements",
                newName: "IX_TBL_InventoryMovements_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryMovements_ProductId",
                table: "TBL_InventoryMovements",
                newName: "IX_TBL_InventoryMovements_ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryMovements_CreatedAt",
                table: "TBL_InventoryMovements",
                newName: "IX_TBL_InventoryMovements_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Categories_Name",
                table: "TBL_Categories",
                newName: "IX_TBL_Categories_Name");

            migrationBuilder.RenameIndex(
                name: "IX_Carts_UserId",
                table: "TBL_Carts",
                newName: "IX_TBL_Carts_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_CartItems_ProductId",
                table: "TBL_CartItems",
                newName: "IX_TBL_CartItems_ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_CartItems_CartId_ProductId",
                table: "TBL_CartItems",
                newName: "IX_TBL_CartItems_CartId_ProductId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TBL_Users",
                table: "TBL_Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TBL_ShippingAddresses",
                table: "TBL_ShippingAddresses",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TBL_Roles",
                table: "TBL_Roles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TBL_RefreshTokens",
                table: "TBL_RefreshTokens",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TBL_Products",
                table: "TBL_Products",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TBL_Payments",
                table: "TBL_Payments",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TBL_Orders",
                table: "TBL_Orders",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TBL_OrderItems",
                table: "TBL_OrderItems",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TBL_InventoryMovements",
                table: "TBL_InventoryMovements",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TBL_Inventories",
                table: "TBL_Inventories",
                column: "ProductId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TBL_Categories",
                table: "TBL_Categories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TBL_Carts",
                table: "TBL_Carts",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TBL_CartItems",
                table: "TBL_CartItems",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_CartItems_TBL_Carts_CartId",
                table: "TBL_CartItems",
                column: "CartId",
                principalTable: "TBL_Carts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_CartItems_TBL_Products_ProductId",
                table: "TBL_CartItems",
                column: "ProductId",
                principalTable: "TBL_Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_Carts_TBL_Users_UserId",
                table: "TBL_Carts",
                column: "UserId",
                principalTable: "TBL_Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_Inventories_TBL_Products_ProductId",
                table: "TBL_Inventories",
                column: "ProductId",
                principalTable: "TBL_Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_InventoryMovements_TBL_Products_ProductId",
                table: "TBL_InventoryMovements",
                column: "ProductId",
                principalTable: "TBL_Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_InventoryMovements_TBL_Users_UserId",
                table: "TBL_InventoryMovements",
                column: "UserId",
                principalTable: "TBL_Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_OrderItems_TBL_Orders_OrderId",
                table: "TBL_OrderItems",
                column: "OrderId",
                principalTable: "TBL_Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_OrderItems_TBL_Products_ProductId",
                table: "TBL_OrderItems",
                column: "ProductId",
                principalTable: "TBL_Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_Orders_TBL_Users_UserId",
                table: "TBL_Orders",
                column: "UserId",
                principalTable: "TBL_Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_Payments_TBL_Orders_OrderId",
                table: "TBL_Payments",
                column: "OrderId",
                principalTable: "TBL_Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_Products_TBL_Categories_CategoryId",
                table: "TBL_Products",
                column: "CategoryId",
                principalTable: "TBL_Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_RefreshTokens_TBL_Users_UserId",
                table: "TBL_RefreshTokens",
                column: "UserId",
                principalTable: "TBL_Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_ShippingAddresses_TBL_Users_UserId",
                table: "TBL_ShippingAddresses",
                column: "UserId",
                principalTable: "TBL_Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_Users_TBL_Roles_RoleId",
                table: "TBL_Users",
                column: "RoleId",
                principalTable: "TBL_Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TBL_CartItems_TBL_Carts_CartId",
                table: "TBL_CartItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_CartItems_TBL_Products_ProductId",
                table: "TBL_CartItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_Carts_TBL_Users_UserId",
                table: "TBL_Carts");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_Inventories_TBL_Products_ProductId",
                table: "TBL_Inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_InventoryMovements_TBL_Products_ProductId",
                table: "TBL_InventoryMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_InventoryMovements_TBL_Users_UserId",
                table: "TBL_InventoryMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_OrderItems_TBL_Orders_OrderId",
                table: "TBL_OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_OrderItems_TBL_Products_ProductId",
                table: "TBL_OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_Orders_TBL_Users_UserId",
                table: "TBL_Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_Payments_TBL_Orders_OrderId",
                table: "TBL_Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_Products_TBL_Categories_CategoryId",
                table: "TBL_Products");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_RefreshTokens_TBL_Users_UserId",
                table: "TBL_RefreshTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_ShippingAddresses_TBL_Users_UserId",
                table: "TBL_ShippingAddresses");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_Users_TBL_Roles_RoleId",
                table: "TBL_Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TBL_Users",
                table: "TBL_Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TBL_ShippingAddresses",
                table: "TBL_ShippingAddresses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TBL_Roles",
                table: "TBL_Roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TBL_RefreshTokens",
                table: "TBL_RefreshTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TBL_Products",
                table: "TBL_Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TBL_Payments",
                table: "TBL_Payments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TBL_Orders",
                table: "TBL_Orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TBL_OrderItems",
                table: "TBL_OrderItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TBL_InventoryMovements",
                table: "TBL_InventoryMovements");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TBL_Inventories",
                table: "TBL_Inventories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TBL_Categories",
                table: "TBL_Categories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TBL_Carts",
                table: "TBL_Carts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TBL_CartItems",
                table: "TBL_CartItems");

            migrationBuilder.RenameTable(
                name: "TBL_Users",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "TBL_ShippingAddresses",
                newName: "ShippingAddresses");

            migrationBuilder.RenameTable(
                name: "TBL_Roles",
                newName: "Roles");

            migrationBuilder.RenameTable(
                name: "TBL_RefreshTokens",
                newName: "RefreshTokens");

            migrationBuilder.RenameTable(
                name: "TBL_Products",
                newName: "Products");

            migrationBuilder.RenameTable(
                name: "TBL_Payments",
                newName: "Payments");

            migrationBuilder.RenameTable(
                name: "TBL_Orders",
                newName: "Orders");

            migrationBuilder.RenameTable(
                name: "TBL_OrderItems",
                newName: "OrderItems");

            migrationBuilder.RenameTable(
                name: "TBL_InventoryMovements",
                newName: "InventoryMovements");

            migrationBuilder.RenameTable(
                name: "TBL_Inventories",
                newName: "Inventories");

            migrationBuilder.RenameTable(
                name: "TBL_Categories",
                newName: "Categories");

            migrationBuilder.RenameTable(
                name: "TBL_Carts",
                newName: "Carts");

            migrationBuilder.RenameTable(
                name: "TBL_CartItems",
                newName: "CartItems");

            migrationBuilder.RenameIndex(
                name: "IX_TBL_Users_RoleId",
                table: "Users",
                newName: "IX_Users_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_TBL_Users_EmailHash",
                table: "Users",
                newName: "IX_Users_EmailHash");

            migrationBuilder.RenameIndex(
                name: "IX_TBL_ShippingAddresses_UserId",
                table: "ShippingAddresses",
                newName: "IX_ShippingAddresses_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_TBL_Roles_Name",
                table: "Roles",
                newName: "IX_Roles_Name");

            migrationBuilder.RenameIndex(
                name: "IX_TBL_RefreshTokens_UserId",
                table: "RefreshTokens",
                newName: "IX_RefreshTokens_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_TBL_RefreshTokens_Token",
                table: "RefreshTokens",
                newName: "IX_RefreshTokens_Token");

            migrationBuilder.RenameIndex(
                name: "IX_TBL_Products_SKU",
                table: "Products",
                newName: "IX_Products_SKU");

            migrationBuilder.RenameIndex(
                name: "IX_TBL_Products_Price",
                table: "Products",
                newName: "IX_Products_Price");

            migrationBuilder.RenameIndex(
                name: "IX_TBL_Products_IsActive",
                table: "Products",
                newName: "IX_Products_IsActive");

            migrationBuilder.RenameIndex(
                name: "IX_TBL_Products_CategoryId",
                table: "Products",
                newName: "IX_Products_CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_TBL_Payments_OrderId",
                table: "Payments",
                newName: "IX_Payments_OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_TBL_Payments_IdempotencyKey",
                table: "Payments",
                newName: "IX_Payments_IdempotencyKey");

            migrationBuilder.RenameIndex(
                name: "IX_TBL_Orders_UserId",
                table: "Orders",
                newName: "IX_Orders_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_TBL_Orders_Status",
                table: "Orders",
                newName: "IX_Orders_Status");

            migrationBuilder.RenameIndex(
                name: "IX_TBL_Orders_OrderNumber",
                table: "Orders",
                newName: "IX_Orders_OrderNumber");

            migrationBuilder.RenameIndex(
                name: "IX_TBL_Orders_CreatedAt",
                table: "Orders",
                newName: "IX_Orders_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_TBL_OrderItems_ProductId",
                table: "OrderItems",
                newName: "IX_OrderItems_ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_TBL_OrderItems_OrderId",
                table: "OrderItems",
                newName: "IX_OrderItems_OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_TBL_InventoryMovements_UserId",
                table: "InventoryMovements",
                newName: "IX_InventoryMovements_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_TBL_InventoryMovements_ProductId",
                table: "InventoryMovements",
                newName: "IX_InventoryMovements_ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_TBL_InventoryMovements_CreatedAt",
                table: "InventoryMovements",
                newName: "IX_InventoryMovements_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_TBL_Categories_Name",
                table: "Categories",
                newName: "IX_Categories_Name");

            migrationBuilder.RenameIndex(
                name: "IX_TBL_Carts_UserId",
                table: "Carts",
                newName: "IX_Carts_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_TBL_CartItems_ProductId",
                table: "CartItems",
                newName: "IX_CartItems_ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_TBL_CartItems_CartId_ProductId",
                table: "CartItems",
                newName: "IX_CartItems_CartId_ProductId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ShippingAddresses",
                table: "ShippingAddresses",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Roles",
                table: "Roles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RefreshTokens",
                table: "RefreshTokens",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Products",
                table: "Products",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Payments",
                table: "Payments",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Orders",
                table: "Orders",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderItems",
                table: "OrderItems",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InventoryMovements",
                table: "InventoryMovements",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Inventories",
                table: "Inventories",
                column: "ProductId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Categories",
                table: "Categories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Carts",
                table: "Carts",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CartItems",
                table: "CartItems",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_Carts_CartId",
                table: "CartItems",
                column: "CartId",
                principalTable: "Carts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_Products_ProductId",
                table: "CartItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Carts_Users_UserId",
                table: "Carts",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Products_ProductId",
                table: "Inventories",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryMovements_Products_ProductId",
                table: "InventoryMovements",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryMovements_Users_UserId",
                table: "InventoryMovements",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Orders_OrderId",
                table: "OrderItems",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Products_ProductId",
                table: "OrderItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_UserId",
                table: "Orders",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Orders_OrderId",
                table: "Payments",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_Users_UserId",
                table: "RefreshTokens",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShippingAddresses_Users_UserId",
                table: "ShippingAddresses",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
