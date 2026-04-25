using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserIdFromVendorUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VendorUsers_Users_UserId",
                table: "VendorUsers");

            migrationBuilder.DropIndex(
                name: "IX_VendorUsers_UserId",
                table: "VendorUsers");

            migrationBuilder.DropIndex(
                name: "IX_VendorUsers_VendorId_UserId",
                table: "VendorUsers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "VendorUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "VendorUsers",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "VendorUsers",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_VendorUsers_Email",
                table: "VendorUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorUsers_VendorId_Username",
                table: "VendorUsers",
                columns: new[] { "VendorId", "Username" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VendorUsers_Email",
                table: "VendorUsers");

            migrationBuilder.DropIndex(
                name: "IX_VendorUsers_VendorId_Username",
                table: "VendorUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "VendorUsers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "VendorUsers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "VendorUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_VendorUsers_UserId",
                table: "VendorUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorUsers_VendorId_UserId",
                table: "VendorUsers",
                columns: new[] { "VendorId", "UserId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_VendorUsers_Users_UserId",
                table: "VendorUsers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
