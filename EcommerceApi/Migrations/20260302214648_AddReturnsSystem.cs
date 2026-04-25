using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnsSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderId",
                table: "VendorWalletTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrderItemId",
                table: "VendorWalletTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReturnRequestId",
                table: "VendorWalletTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RefundedQuantity",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ReturnStatus",
                table: "OrderItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ReturnRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomerComment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdminUserId = table.Column<int>(type: "int", nullable: true),
                    AdminNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RefundMethod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RefundReference = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReturnRequests_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReturnRequests_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReturnItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReturnRequestId = table.Column<int>(type: "int", nullable: false),
                    OrderItemId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    VendorId = table.Column<int>(type: "int", nullable: false),
                    QtyRequested = table.Column<int>(type: "int", nullable: false),
                    QtyApproved = table.Column<int>(type: "int", nullable: false),
                    QtyReceived = table.Column<int>(type: "int", nullable: false),
                    UnitPriceSnapshot = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VendorAmountSnapshot = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PlatformFeeSnapshot = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CommissionRateSnapshot = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CommissionAmountSnapshot = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VendorNetAmountSnapshot = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RefundLineAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ConditionReceived = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RestockAction = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReturnItems_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReturnItems_ReturnRequests_ReturnRequestId",
                        column: x => x.ReturnRequestId,
                        principalTable: "ReturnRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnItems_OrderItemId",
                table: "ReturnItems",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnItems_ReturnRequestId",
                table: "ReturnItems",
                column: "ReturnRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_CustomerId",
                table: "ReturnRequests",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_OrderId",
                table: "ReturnRequests",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_Status",
                table: "ReturnRequests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReturnItems");

            migrationBuilder.DropTable(
                name: "ReturnRequests");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "VendorWalletTransactions");

            migrationBuilder.DropColumn(
                name: "OrderItemId",
                table: "VendorWalletTransactions");

            migrationBuilder.DropColumn(
                name: "ReturnRequestId",
                table: "VendorWalletTransactions");

            migrationBuilder.DropColumn(
                name: "RefundedQuantity",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ReturnStatus",
                table: "OrderItems");
        }
    }
}
