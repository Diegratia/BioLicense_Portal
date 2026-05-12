using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioLicense_Portal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addTierConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "tier_configs",
                table: "applications",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tier_configs",
                table: "applications");
        }
    }
}
