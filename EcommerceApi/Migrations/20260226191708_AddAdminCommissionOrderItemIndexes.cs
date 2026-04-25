using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminCommissionOrderItemIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
 

            migrationBuilder.AddColumn<int>(
                name: "OrderItemId",
                table: "AdminWalletTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminWalletTransactions_OrderItemId",
                table: "AdminWalletTransactions",
                column: "OrderItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_AdminWalletTransactions_OrderItems_OrderItemId",
                table: "AdminWalletTransactions",
                column: "OrderItemId",
                principalTable: "OrderItems",
                principalColumn: "Id");

            migrationBuilder.Sql(@"
IF NOT EXISTS (
  SELECT 1 FROM sys.indexes
  WHERE name = 'UX_VendorPayoutSales_OrderItemId'
    AND object_id = OBJECT_ID('dbo.VendorPayoutSales')
)
BEGIN
  CREATE UNIQUE INDEX UX_VendorPayoutSales_OrderItemId
  ON dbo.VendorPayoutSales(OrderItemId);
END
");

            migrationBuilder.Sql(@"
CREATE UNIQUE INDEX UX_AdminWallet_Commission_OrderItem
ON dbo.AdminWalletTransactions(OrderItemId)
WHERE Type = 'Commission' AND OrderItemId IS NOT NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdminWalletTransactions_OrderItems_OrderItemId",
                table: "AdminWalletTransactions");

            migrationBuilder.DropIndex(
                name: "IX_AdminWalletTransactions_OrderItemId",
                table: "AdminWalletTransactions");


            migrationBuilder.DropColumn(
                name: "OrderItemId",
                table: "AdminWalletTransactions");
        }
    }
}
