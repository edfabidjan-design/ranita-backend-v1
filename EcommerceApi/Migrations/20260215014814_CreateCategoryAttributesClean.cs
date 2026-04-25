using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class CreateCategoryAttributesClean : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ProductId",
                table: "ProductVariants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CategoryAttributes",
                table: "CategoryAttributes");

            migrationBuilder.DropIndex(
                name: "IX_CategoryAttributes_CategoryId_AttributeId",
                table: "CategoryAttributes");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "CategoryAttributes");

            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.RenameTable(
                name: "ProductAttributeValues",
                newName: "ProductAttributeValues",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "CategoryAttributes",
                newName: "CategoryAttributes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "Attributes",
                newName: "Attributes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "AttributeOptions",
                newName: "AttributeOptions",
                newSchema: "dbo");

            migrationBuilder.AddColumn<string>(
                name: "Key1",
                table: "ProductVariants",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Key2",
                table: "ProductVariants",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "AttributeDefId",
                schema: "dbo",
                table: "ProductAttributeValues",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "dbo",
                table: "AttributeOptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_CategoryAttributes",
                schema: "dbo",
                table: "CategoryAttributes",
                columns: new[] { "CategoryId", "AttributeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_Key1_Key2",
                table: "ProductVariants",
                columns: new[] { "ProductId", "Key1", "Key2" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ProductId_Key1_Key2",
                table: "ProductVariants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CategoryAttributes",
                schema: "dbo",
                table: "CategoryAttributes");

            migrationBuilder.DropColumn(
                name: "Key1",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "Key2",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "AttributeDefId",
                schema: "dbo",
                table: "ProductAttributeValues");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "dbo",
                table: "AttributeOptions");

            migrationBuilder.RenameTable(
                name: "ProductAttributeValues",
                schema: "dbo",
                newName: "ProductAttributeValues");

            migrationBuilder.RenameTable(
                name: "CategoryAttributes",
                schema: "dbo",
                newName: "CategoryAttributes");

            migrationBuilder.RenameTable(
                name: "Attributes",
                schema: "dbo",
                newName: "Attributes");

            migrationBuilder.RenameTable(
                name: "AttributeOptions",
                schema: "dbo",
                newName: "AttributeOptions");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "CategoryAttributes",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CategoryAttributes",
                table: "CategoryAttributes",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId",
                table: "ProductVariants",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryAttributes_CategoryId_AttributeId",
                table: "CategoryAttributes",
                columns: new[] { "CategoryId", "AttributeId" },
                unique: true);
        }
    }
}
