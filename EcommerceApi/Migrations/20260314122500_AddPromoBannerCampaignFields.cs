using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPromoBannerCampaignFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BadgeText",
                table: "PromoBanners");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "PromoBanners",
                newName: "StartAt");

            migrationBuilder.RenameColumn(
                name: "EndDate",
                table: "PromoBanners",
                newName: "EndAt");

            migrationBuilder.AddColumn<string>(
                name: "AccentColor",
                table: "PromoBanners",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BackgroundImageUrl",
                table: "PromoBanners",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Position",
                table: "PromoBanners",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "PromoBanners",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PromoCode",
                table: "PromoBanners",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SideText",
                table: "PromoBanners",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SideTitle",
                table: "PromoBanners",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Theme",
                table: "PromoBanners",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccentColor",
                table: "PromoBanners");

            migrationBuilder.DropColumn(
                name: "BackgroundImageUrl",
                table: "PromoBanners");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "PromoBanners");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "PromoBanners");

            migrationBuilder.DropColumn(
                name: "PromoCode",
                table: "PromoBanners");

            migrationBuilder.DropColumn(
                name: "SideText",
                table: "PromoBanners");

            migrationBuilder.DropColumn(
                name: "SideTitle",
                table: "PromoBanners");

            migrationBuilder.DropColumn(
                name: "Theme",
                table: "PromoBanners");

            migrationBuilder.RenameColumn(
                name: "StartAt",
                table: "PromoBanners",
                newName: "StartDate");

            migrationBuilder.RenameColumn(
                name: "EndAt",
                table: "PromoBanners",
                newName: "EndDate");

            migrationBuilder.AddColumn<string>(
                name: "BadgeText",
                table: "PromoBanners",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);
        }
    }
}