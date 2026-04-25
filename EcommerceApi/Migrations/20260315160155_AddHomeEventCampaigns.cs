using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddHomeEventCampaigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HomeEventCampaigns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Subtitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    BadgeText = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    DesktopImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MobileImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ButtonText = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ButtonLink = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TargetType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "url"),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    BackgroundColor = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    TextColor = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeEventCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HomeEventCampaigns_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HomeEventCampaigns_CategoryId",
                table: "HomeEventCampaigns",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_HomeEventCampaigns_DisplayOrder",
                table: "HomeEventCampaigns",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_HomeEventCampaigns_IsActive",
                table: "HomeEventCampaigns",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_HomeEventCampaigns_IsFeatured",
                table: "HomeEventCampaigns",
                column: "IsFeatured");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomeEventCampaigns");
        }
    }
}
