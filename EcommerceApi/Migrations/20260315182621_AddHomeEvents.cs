using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddHomeEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HomeEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Subtitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    BadgeText = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    DesktopImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MobileImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ButtonText = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ButtonLink = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TargetType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    BackgroundColor = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    TextColor = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HomeEvents_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HomeEvents_CategoryId",
                table: "HomeEvents",
                column: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomeEvents");
        }
    }
}
