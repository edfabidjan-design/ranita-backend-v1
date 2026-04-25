using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddHeroSlides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HeroSlides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    Subtitle = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: true),
                    BadgeText = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SmallTag = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PrimaryButtonText = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    PrimaryButtonUrl = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    SecondaryButtonText = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    SecondaryButtonUrl = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Theme = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AccentColor = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    HighlightText = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeroSlides", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HeroSlides_IsActive_DisplayOrder",
                table: "HeroSlides",
                columns: new[] { "IsActive", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HeroSlides");
        }
    }
}
