using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddRejectReasonToProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                name: "CategoryAttributeAttributeId",
                schema: "dbo",
                table: "CategoryAttributes");

            migrationBuilder.DropColumn(
                name: "CategoryAttributeCategoryId",
                schema: "dbo",
                table: "CategoryAttributes");

            migrationBuilder.AddColumn<string>(
                name: "RejectReason",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "Products",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectReason",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "Products");

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
    }
}
