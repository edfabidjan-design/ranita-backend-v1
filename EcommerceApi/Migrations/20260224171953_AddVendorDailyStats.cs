using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorDailyStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VendorDailyStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VendorId = table.Column<int>(type: "int", nullable: false),
                    Day = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeliveredOrdersCount = table.Column<int>(type: "int", nullable: false),
                    DeliveredItemsCount = table.Column<int>(type: "int", nullable: false),
                    DeliveredRevenue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorDailyStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorDailyStats_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VendorProductDailyStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VendorId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Day = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QtySold = table.Column<int>(type: "int", nullable: false),
                    Revenue = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorProductDailyStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorProductDailyStats_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VendorProductDailyStats_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VendorDailyStats_VendorId_Day",
                table: "VendorDailyStats",
                columns: new[] { "VendorId", "Day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorProductDailyStats_ProductId",
                table: "VendorProductDailyStats",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorProductDailyStats_VendorId_ProductId_Day",
                table: "VendorProductDailyStats",
                columns: new[] { "VendorId", "ProductId", "Day" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VendorDailyStats");

            migrationBuilder.DropTable(
                name: "VendorProductDailyStats");
        }
    }
}
