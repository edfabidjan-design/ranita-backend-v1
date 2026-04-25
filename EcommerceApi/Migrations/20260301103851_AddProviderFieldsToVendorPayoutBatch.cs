using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderFieldsToVendorPayoutBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VendorPayouts_Vendors_VendorId",
                table: "VendorPayouts");

            migrationBuilder.AddColumn<string>(
                name: "ProviderRef",
                table: "VendorPayouts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "VendorPayoutBatches",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "VendorPayoutBatches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderRef",
                table: "VendorPayoutBatches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CommissionRate",
                table: "OrderItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddForeignKey(
                name: "FK_VendorPayouts_Vendors_VendorId",
                table: "VendorPayouts",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VendorPayouts_Vendors_VendorId",
                table: "VendorPayouts");

            migrationBuilder.DropColumn(
                name: "ProviderRef",
                table: "VendorPayouts");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "VendorPayoutBatches");

            migrationBuilder.DropColumn(
                name: "ProviderRef",
                table: "VendorPayoutBatches");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "VendorPayoutBatches",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<decimal>(
                name: "CommissionRate",
                table: "OrderItems",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AddForeignKey(
                name: "FK_VendorPayouts_Vendors_VendorId",
                table: "VendorPayouts",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
