using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    public partial class AddHomePromoBanner : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HomePromoBanners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Subtitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PromoCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrimaryButtonText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrimaryButtonUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecondaryButtonText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecondaryButtonUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SideTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SideText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomePromoBanners", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomePromoBanners");
        }
    }
}