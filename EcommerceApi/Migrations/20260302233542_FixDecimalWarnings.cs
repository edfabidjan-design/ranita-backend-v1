using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class FixDecimalWarnings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReturnImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReturnRequestId = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReturnImageId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReturnImages_ReturnImages_ReturnImageId",
                        column: x => x.ReturnImageId,
                        principalTable: "ReturnImages",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReturnImages_ReturnRequests_ReturnRequestId",
                        column: x => x.ReturnRequestId,
                        principalTable: "ReturnRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnImages_ReturnImageId",
                table: "ReturnImages",
                column: "ReturnImageId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnImages_ReturnRequestId",
                table: "ReturnImages",
                column: "ReturnRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReturnImages");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "Orders");
        }
    }
}
