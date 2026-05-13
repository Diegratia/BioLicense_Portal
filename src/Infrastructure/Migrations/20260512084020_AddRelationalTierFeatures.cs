using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioLicense_Portal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRelationalTierFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_application_tiers_application_id",
                table: "application_tiers");

            migrationBuilder.DropColumn(
                name: "features",
                table: "application_tiers");

            migrationBuilder.CreateTable(
                name: "application_tier_features",
                columns: table => new
                {
                    tier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    feature_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_application_tier_features", x => new { x.tier_id, x.feature_id });
                    table.ForeignKey(
                        name: "FK_application_tier_features_application_features_feature_id",
                        column: x => x.feature_id,
                        principalTable: "application_features",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_application_tier_features_application_tiers_tier_id",
                        column: x => x.tier_id,
                        principalTable: "application_tiers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_application_tiers_application_id_tier",
                table: "application_tiers",
                columns: new[] { "application_id", "tier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_application_tier_features_feature_id",
                table: "application_tier_features",
                column: "feature_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "application_tier_features");

            migrationBuilder.DropIndex(
                name: "IX_application_tiers_application_id_tier",
                table: "application_tiers");

            migrationBuilder.AddColumn<string>(
                name: "features",
                table: "application_tiers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_application_tiers_application_id",
                table: "application_tiers",
                column: "application_id");
        }
    }
}
