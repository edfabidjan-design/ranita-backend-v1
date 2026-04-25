using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddHeroSlideRightContentAndStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RightBadgeText",
                table: "HeroSlides",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RightButtonText",
                table: "HeroSlides",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RightButtonUrl",
                table: "HeroSlides",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RightSubtitle",
                table: "HeroSlides",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RightTitle",
                table: "HeroSlides",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Stat1Label",
                table: "HeroSlides",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Stat1Value",
                table: "HeroSlides",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Stat2Label",
                table: "HeroSlides",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Stat2Value",
                table: "HeroSlides",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Stat3Label",
                table: "HeroSlides",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Stat3Value",
                table: "HeroSlides",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatsTitle",
                table: "HeroSlides",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RightBadgeText",
                table: "HeroSlides");

            migrationBuilder.DropColumn(
                name: "RightButtonText",
                table: "HeroSlides");

            migrationBuilder.DropColumn(
                name: "RightButtonUrl",
                table: "HeroSlides");

            migrationBuilder.DropColumn(
                name: "RightSubtitle",
                table: "HeroSlides");

            migrationBuilder.DropColumn(
                name: "RightTitle",
                table: "HeroSlides");

            migrationBuilder.DropColumn(
                name: "Stat1Label",
                table: "HeroSlides");

            migrationBuilder.DropColumn(
                name: "Stat1Value",
                table: "HeroSlides");

            migrationBuilder.DropColumn(
                name: "Stat2Label",
                table: "HeroSlides");

            migrationBuilder.DropColumn(
                name: "Stat2Value",
                table: "HeroSlides");

            migrationBuilder.DropColumn(
                name: "Stat3Label",
                table: "HeroSlides");

            migrationBuilder.DropColumn(
                name: "Stat3Value",
                table: "HeroSlides");

            migrationBuilder.DropColumn(
                name: "StatsTitle",
                table: "HeroSlides");
        }
    }
}
