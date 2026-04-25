using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class FixAttributeDataType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductAttributeValues_ProductId",
                table: "ProductAttributeValues");

            migrationBuilder.DropIndex(
                name: "IX_ProductAttributeValues_ProductVariantId",
                table: "ProductAttributeValues");

            migrationBuilder.DropIndex(
                name: "IX_CategoryAttributes_CategoryId",
                table: "CategoryAttributes");

            migrationBuilder.DropIndex(
                name: "IX_AttributeOptions_AttributeId",
                table: "AttributeOptions");

            migrationBuilder.AlterColumn<string>(
                name: "DataType",
                table: "Attributes",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Attributes",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "AttributeOptions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeValues_ProductId_AttributeId",
                table: "ProductAttributeValues",
                columns: new[] { "ProductId", "AttributeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeValues_ProductVariantId_AttributeId",
                table: "ProductAttributeValues",
                columns: new[] { "ProductVariantId", "AttributeId" });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryAttributes_CategoryId_AttributeId",
                table: "CategoryAttributes",
                columns: new[] { "CategoryId", "AttributeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attributes_Code",
                table: "Attributes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttributeOptions_AttributeId_Value",
                table: "AttributeOptions",
                columns: new[] { "AttributeId", "Value" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductAttributeValues_ProductId_AttributeId",
                table: "ProductAttributeValues");

            migrationBuilder.DropIndex(
                name: "IX_ProductAttributeValues_ProductVariantId_AttributeId",
                table: "ProductAttributeValues");

            migrationBuilder.DropIndex(
                name: "IX_CategoryAttributes_CategoryId_AttributeId",
                table: "CategoryAttributes");

            migrationBuilder.DropIndex(
                name: "IX_Attributes_Code",
                table: "Attributes");

            migrationBuilder.DropIndex(
                name: "IX_AttributeOptions_AttributeId_Value",
                table: "AttributeOptions");

            migrationBuilder.AlterColumn<string>(
                name: "DataType",
                table: "Attributes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Attributes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "AttributeOptions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeValues_ProductId",
                table: "ProductAttributeValues",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeValues_ProductVariantId",
                table: "ProductAttributeValues",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryAttributes_CategoryId",
                table: "CategoryAttributes",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AttributeOptions_AttributeId",
                table: "AttributeOptions",
                column: "AttributeId");
        }
    }
}
