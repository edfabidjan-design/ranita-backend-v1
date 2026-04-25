using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddIsVariantToCategoryAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ProductVariants",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ProductVariants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "dbo",
                table: "ProductAttributeValues",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "dbo",
                table: "ProductAttributeValues",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CategoryAttributeAttributeId",
                schema: "dbo",
                table: "CategoryAttributes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CategoryAttributeCategoryId",
                schema: "dbo",
                table: "CategoryAttributes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVariant",
                schema: "dbo",
                table: "CategoryAttributes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_CategoryAttributes_CategoryAttributeCategoryId_CategoryAttributeAttributeId",
                schema: "dbo",
                table: "CategoryAttributes",
                columns: new[] { "CategoryAttributeCategoryId", "CategoryAttributeAttributeId" });

            migrationBuilder.AddForeignKey(
                name: "FK_CategoryAttributes_CategoryAttributes_CategoryAttributeCategoryId_CategoryAttributeAttributeId",
                schema: "dbo",
                table: "CategoryAttributes",
                columns: new[] { "CategoryAttributeCategoryId", "CategoryAttributeAttributeId" },
                principalSchema: "dbo",
                principalTable: "CategoryAttributes",
                principalColumns: new[] { "CategoryId", "AttributeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CategoryAttributes_CategoryAttributes_CategoryAttributeCategoryId_CategoryAttributeAttributeId",
                schema: "dbo",
                table: "CategoryAttributes");

            migrationBuilder.DropIndex(
                name: "IX_CategoryAttributes_CategoryAttributeCategoryId_CategoryAttributeAttributeId",
                schema: "dbo",
                table: "CategoryAttributes");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "dbo",
                table: "ProductAttributeValues");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "dbo",
                table: "ProductAttributeValues");

            migrationBuilder.DropColumn(
                name: "CategoryAttributeAttributeId",
                schema: "dbo",
                table: "CategoryAttributes");

            migrationBuilder.DropColumn(
                name: "CategoryAttributeCategoryId",
                schema: "dbo",
                table: "CategoryAttributes");

            migrationBuilder.DropColumn(
                name: "IsVariant",
                schema: "dbo",
                table: "CategoryAttributes");
        }
    }
}
