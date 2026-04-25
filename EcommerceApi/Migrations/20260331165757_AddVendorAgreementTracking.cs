using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorAgreementTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SignedAgreementReceived",
                table: "Vendors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SignedAgreementReceivedAt",
                table: "Vendors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TermsEmailSent",
                table: "Vendors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TermsEmailSentAt",
                table: "Vendors",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SignedAgreementReceived",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "SignedAgreementReceivedAt",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "TermsEmailSent",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "TermsEmailSentAt",
                table: "Vendors");
        }
    }
}
