using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketplaceOrderSplit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems");

            migrationBuilder.AddColumn<decimal>(
                name: "PlatformFee",
                table: "OrderItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPriceSnapshot",
                table: "OrderItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VendorAmount",
                table: "OrderItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "VendorId",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VendorStatus",
                table: "OrderItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId_VendorId",
                table: "OrderItems",
                columns: new[] { "OrderId", "VendorId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_VendorId",
                table: "OrderItems",
                column: "VendorId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Vendors_VendorId",
                table: "OrderItems",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Vendors_VendorId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_OrderId_VendorId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_VendorId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "PlatformFee",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "UnitPriceSnapshot",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "VendorAmount",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "VendorId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "VendorStatus",
                table: "OrderItems");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");
        }
    }
}
