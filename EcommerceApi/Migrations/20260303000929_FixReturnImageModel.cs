using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class FixReturnImageModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReturnImages_ReturnImages_ReturnImageId",
                table: "ReturnImages");

            migrationBuilder.DropIndex(
                name: "IX_ReturnImages_ReturnImageId",
                table: "ReturnImages");

            migrationBuilder.DropColumn(
                name: "ReturnImageId",
                table: "ReturnImages");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ReturnImages",
                newName: "CreatedAtUtc");

            migrationBuilder.AlterColumn<decimal>(
                name: "CommissionRateSnapshot",
                table: "ReturnItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "ReturnImages",
                newName: "CreatedAt");

            migrationBuilder.AlterColumn<decimal>(
                name: "CommissionRateSnapshot",
                table: "ReturnItems",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AddColumn<int>(
                name: "ReturnImageId",
                table: "ReturnImages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnImages_ReturnImageId",
                table: "ReturnImages",
                column: "ReturnImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnImages_ReturnImages_ReturnImageId",
                table: "ReturnImages",
                column: "ReturnImageId",
                principalTable: "ReturnImages",
                principalColumn: "Id");
        }
    }
}
