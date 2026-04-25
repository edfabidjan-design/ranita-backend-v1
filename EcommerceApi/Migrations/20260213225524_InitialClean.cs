using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialClean : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ProductId_Size_Color",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "Sku",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "VariantMode",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ColorSnapshot",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "Dimensions",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "NameSnapshot",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "PriceSnapshot",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "Qty",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "SizeSnapshot",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "Sku",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "WeightKg",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "VariantMode",
                table: "Categories");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "ProductVariants",
                newName: "PriceOverride");

            migrationBuilder.AlterColumn<string>(
                name: "Size",
                table: "ProductVariants",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Color",
                table: "ProductVariants",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateTable(
                name: "VariantAxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariantAxes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VariantValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AxisId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariantValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VariantValues_VariantAxes_AxisId",
                        column: x => x.AxisId,
                        principalTable: "VariantAxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariantValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductVariantId = table.Column<int>(type: "int", nullable: false),
                    AxisId = table.Column<int>(type: "int", nullable: false),
                    ValueId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariantValues_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductVariantValues_VariantAxes_AxisId",
                        column: x => x.AxisId,
                        principalTable: "VariantAxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductVariantValues_VariantValues_ValueId",
                        column: x => x.ValueId,
                        principalTable: "VariantValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId",
                table: "ProductVariants",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantValues_AxisId",
                table: "ProductVariantValues",
                column: "AxisId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantValues_ProductVariantId_AxisId",
                table: "ProductVariantValues",
                columns: new[] { "ProductVariantId", "AxisId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantValues_ValueId",
                table: "ProductVariantValues",
                column: "ValueId");

            migrationBuilder.CreateIndex(
                name: "IX_VariantAxes_Key",
                table: "VariantAxes",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VariantValues_AxisId_Value",
                table: "VariantValues",
                columns: new[] { "AxisId", "Value" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductVariantValues");

            migrationBuilder.DropTable(
                name: "VariantValues");

            migrationBuilder.DropTable(
                name: "VariantAxes");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ProductId",
                table: "ProductVariants");

            migrationBuilder.RenameColumn(
                name: "PriceOverride",
                table: "ProductVariants",
                newName: "Price");

            migrationBuilder.AlterColumn<string>(
                name: "Size",
                table: "ProductVariants",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Color",
                table: "ProductVariants",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ProductVariants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Sku",
                table: "ProductVariants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantMode",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ColorSnapshot",
                table: "OrderItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Dimensions",
                table: "OrderItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameSnapshot",
                table: "OrderItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PriceSnapshot",
                table: "OrderItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Qty",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SizeSnapshot",
                table: "OrderItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sku",
                table: "OrderItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightKg",
                table: "OrderItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantMode",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_Size_Color",
                table: "ProductVariants",
                columns: new[] { "ProductId", "Size", "Color" },
                unique: true);
        }
    }
}
