using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class FixVendorPayoutSaleRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VendorPayoutSales_VendorPayouts_PayoutId",
                table: "VendorPayoutSales");

            migrationBuilder.RenameColumn(
                name: "PayoutId",
                table: "VendorPayoutSales",
                newName: "VendorPayoutId");

            migrationBuilder.RenameIndex(
                name: "IX_VendorPayoutSales_PayoutId",
                table: "VendorPayoutSales",
                newName: "IX_VendorPayoutSales_VendorPayoutId");

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "VendorPayoutSales",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "VendorPayoutSales",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_VendorPayoutSales_OrderItemId",
                table: "VendorPayoutSales",
                column: "OrderItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_VendorPayoutSales_OrderItems_OrderItemId",
                table: "VendorPayoutSales",
                column: "OrderItemId",
                principalTable: "OrderItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VendorPayoutSales_VendorPayouts_VendorPayoutId",
                table: "VendorPayoutSales",
                column: "VendorPayoutId",
                principalTable: "VendorPayouts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VendorPayoutSales_OrderItems_OrderItemId",
                table: "VendorPayoutSales");

            migrationBuilder.DropForeignKey(
                name: "FK_VendorPayoutSales_VendorPayouts_VendorPayoutId",
                table: "VendorPayoutSales");

            migrationBuilder.DropIndex(
                name: "IX_VendorPayoutSales_OrderItemId",
                table: "VendorPayoutSales");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "VendorPayoutSales");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "VendorPayoutSales");

            migrationBuilder.RenameColumn(
                name: "VendorPayoutId",
                table: "VendorPayoutSales",
                newName: "PayoutId");

            migrationBuilder.RenameIndex(
                name: "IX_VendorPayoutSales_VendorPayoutId",
                table: "VendorPayoutSales",
                newName: "IX_VendorPayoutSales_PayoutId");

            migrationBuilder.AddForeignKey(
                name: "FK_VendorPayoutSales_VendorPayouts_PayoutId",
                table: "VendorPayoutSales",
                column: "PayoutId",
                principalTable: "VendorPayouts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
