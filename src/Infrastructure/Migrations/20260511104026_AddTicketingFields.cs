using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioLicense_Portal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "expiry_date",
                table: "license_requests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                table: "license_requests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "expiry_date",
                table: "license_requests");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                table: "license_requests");
        }
    }
}
