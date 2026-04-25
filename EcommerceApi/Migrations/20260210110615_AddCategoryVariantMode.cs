using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryVariantMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VariantMode",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Simple");

            // ✅ remplissage automatique selon le nom de la catégorie
            migrationBuilder.Sql("UPDATE dbo.Categories SET VariantMode = 'Shoes'  WHERE Name LIKE '%Chaussure%'");
            migrationBuilder.Sql("UPDATE dbo.Categories SET VariantMode = 'Watch'  WHERE Name LIKE '%Montre%'");
            migrationBuilder.Sql("UPDATE dbo.Categories SET VariantMode = 'Clothes' WHERE Name LIKE '%Vêtement%' OR Name LIKE '%Vetement%'");
            migrationBuilder.Sql("UPDATE dbo.Categories SET VariantMode = 'Phone'  WHERE Name LIKE '%Téléphone%' OR Name LIKE '%Telephone%'");
            migrationBuilder.Sql("UPDATE dbo.Categories SET VariantMode = 'Pack'   WHERE Name LIKE '%Pack%'");
            migrationBuilder.Sql("UPDATE dbo.Categories SET VariantMode='Simple' WHERE VariantMode IS NULL OR LTRIM(RTRIM(VariantMode))=''");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VariantMode",
                table: "Categories");
        }
    }
}
