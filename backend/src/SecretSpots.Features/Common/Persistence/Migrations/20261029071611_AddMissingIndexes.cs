using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretSpots.Features.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Spots_CreatedByUserId_CreatedAt",
                table: "Spots",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Rewards_BusinessId",
                table: "Rewards",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_RewardRedemptions_UserId_CreatedAt",
                table: "RewardRedemptions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ContentType_ContentId",
                table: "Reports",
                columns: new[] { "ContentType", "ContentId" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_UserId",
                table: "Ratings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_UserId",
                table: "Comments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_SpotId_UserId_CreatedAt",
                table: "CheckIns",
                columns: new[] { "SpotId", "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_UserId_CreatedAt",
                table: "CheckIns",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Spots_CreatedByUserId_CreatedAt",
                table: "Spots");

            migrationBuilder.DropIndex(
                name: "IX_Rewards_BusinessId",
                table: "Rewards");

            migrationBuilder.DropIndex(
                name: "IX_RewardRedemptions_UserId_CreatedAt",
                table: "RewardRedemptions");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ContentType_ContentId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_Ratings_UserId",
                table: "Ratings");

            migrationBuilder.DropIndex(
                name: "IX_Comments_UserId",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_CheckIns_SpotId_UserId_CreatedAt",
                table: "CheckIns");

            migrationBuilder.DropIndex(
                name: "IX_CheckIns_UserId_CreatedAt",
                table: "CheckIns");
        }
    }
}
