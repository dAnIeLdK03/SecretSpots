using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretSpots.Features.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRewardRedemptionFulfillment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FulfilledAt",
                table: "RewardRedemptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FulfilledByUserId",
                table: "RewardRedemptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RedemptionCode",
                table: "RewardRedemptions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FulfilledAt",
                table: "RewardRedemptions");

            migrationBuilder.DropColumn(
                name: "FulfilledByUserId",
                table: "RewardRedemptions");

            migrationBuilder.DropColumn(
                name: "RedemptionCode",
                table: "RewardRedemptions");
        }
    }
}
