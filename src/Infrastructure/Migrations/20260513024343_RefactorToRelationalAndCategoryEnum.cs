using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioLicense_Portal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorToRelationalAndCategoryEnum : Migration
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

            migrationBuilder.RenameColumn(
                name: "Username",
                table: "users",
                newName: "username");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "users",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "users",
                newName: "role");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "users",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "users",
                newName: "password_hash");

            migrationBuilder.RenameColumn(
                name: "LastLoginAt",
                table: "users",
                newName: "last_login_at");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "users",
                newName: "full_name");

            migrationBuilder.RenameIndex(
                name: "IX_users_Username",
                table: "users",
                newName: "IX_users_username");

            migrationBuilder.RenameIndex(
                name: "IX_users_Email",
                table: "users",
                newName: "IX_users_email");

            // Manual data conversion for FeatureCategory
            migrationBuilder.Sql("UPDATE application_features SET category = '0' WHERE category = 'core' OR category = 'Core'");
            migrationBuilder.Sql("UPDATE application_features SET category = '1' WHERE category = 'module' OR category = 'Module'");
            migrationBuilder.Sql("UPDATE application_features SET category = '0' WHERE category NOT IN ('0', '1')");

            // Cleanup duplicate tiers
            migrationBuilder.Sql(@"
                WITH CTE AS (
                    SELECT id, ROW_NUMBER() OVER (PARTITION BY application_id, tier ORDER BY created_at DESC) as rn
                    FROM application_tiers
                )
                DELETE FROM application_tiers WHERE id IN (SELECT id FROM CTE WHERE rn > 1)");

            migrationBuilder.AlterColumn<int>(
                name: "category",
                table: "application_features",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

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
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_application_tier_features_application_tiers_tier_id",
                        column: x => x.tier_id,
                        principalTable: "application_tiers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.NoAction);
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

            migrationBuilder.RenameColumn(
                name: "username",
                table: "users",
                newName: "Username");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "users",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "role",
                table: "users",
                newName: "Role");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "users",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "password_hash",
                table: "users",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "last_login_at",
                table: "users",
                newName: "LastLoginAt");

            migrationBuilder.RenameColumn(
                name: "full_name",
                table: "users",
                newName: "FullName");

            migrationBuilder.RenameIndex(
                name: "IX_users_username",
                table: "users",
                newName: "IX_users_Username");

            migrationBuilder.RenameIndex(
                name: "IX_users_email",
                table: "users",
                newName: "IX_users_Email");

            migrationBuilder.AddColumn<string>(
                name: "features",
                table: "application_tiers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "category",
                table: "application_features",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_application_tiers_application_id",
                table: "application_tiers",
                column: "application_id");
        }
    }
}
