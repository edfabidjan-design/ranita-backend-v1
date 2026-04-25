using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorPayoutBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VendorPayoutId",
                table: "OrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VendorPayoutBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalVendors = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorPayoutBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VendorPayouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<int>(type: "int", nullable: false),
                    VendorId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentRef = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorPayouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorPayouts_VendorPayoutBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "VendorPayoutBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VendorPayouts_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VendorPayoutSales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayoutId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    OrderItemId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Qty = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PlatformFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VendorAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SoldAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorPayoutSales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorPayoutSales_VendorPayouts_PayoutId",
                        column: x => x.PayoutId,
                        principalTable: "VendorPayouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_VendorPayoutId",
                table: "OrderItems",
                column: "VendorPayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorPayoutBatches_PeriodStart_PeriodEnd",
                table: "VendorPayoutBatches",
                columns: new[] { "PeriodStart", "PeriodEnd" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorPayouts_BatchId",
                table: "VendorPayouts",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorPayouts_VendorId",
                table: "VendorPayouts",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorPayoutSales_PayoutId",
                table: "VendorPayoutSales",
                column: "PayoutId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_VendorPayouts_VendorPayoutId",
                table: "OrderItems",
                column: "VendorPayoutId",
                principalTable: "VendorPayouts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_VendorPayouts_VendorPayoutId",
                table: "OrderItems");

            migrationBuilder.DropTable(
                name: "VendorPayoutSales");

            migrationBuilder.DropTable(
                name: "VendorPayouts");

            migrationBuilder.DropTable(
                name: "VendorPayoutBatches");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_VendorPayoutId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "VendorPayoutId",
                table: "OrderItems");
        }
    }
}
