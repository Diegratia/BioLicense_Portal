using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioLicense_Portal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeApplicationTierNameToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "name",
                table: "application_tiers");

            migrationBuilder.AddColumn<int>(
                name: "tier",
                table: "application_tiers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tier",
                table: "application_tiers");

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "application_tiers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
