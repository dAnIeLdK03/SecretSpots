using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretSpots.Features.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRewardAndBusinessActiveFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Defaults to true (not the tool-generated false) — existing rewards/businesses were
            // never paused, so this must not silently deactivate everything already in the DB.
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Rewards",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Businesses",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Rewards");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Businesses");
        }
    }
}
