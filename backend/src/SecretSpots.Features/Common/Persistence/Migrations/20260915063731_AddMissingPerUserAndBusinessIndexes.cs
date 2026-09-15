using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretSpots.Features.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingPerUserAndBusinessIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Rewards_BusinessId_CreatedAt",
                table: "Rewards",
                columns: new[] { "BusinessId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RewardRedemptions_BusinessId_CreatedAt",
                table: "RewardRedemptions",
                columns: new[] { "BusinessId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RewardRedemptions_UserId_CreatedAt",
                table: "RewardRedemptions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_UserId_CreatedAt",
                table: "CheckIns",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_OwnerUserId",
                table: "Businesses",
                column: "OwnerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rewards_BusinessId_CreatedAt",
                table: "Rewards");

            migrationBuilder.DropIndex(
                name: "IX_RewardRedemptions_BusinessId_CreatedAt",
                table: "RewardRedemptions");

            migrationBuilder.DropIndex(
                name: "IX_RewardRedemptions_UserId_CreatedAt",
                table: "RewardRedemptions");

            migrationBuilder.DropIndex(
                name: "IX_CheckIns_UserId_CreatedAt",
                table: "CheckIns");

            migrationBuilder.DropIndex(
                name: "IX_Businesses_OwnerUserId",
                table: "Businesses");
        }
    }
}
