using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretSpots.Features.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SoftDeleteRejectedSpots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "Spots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ScheduledDeletionAt",
                table: "Spots",
                type: "timestamp with time zone",
                nullable: true);

            // Spots rejected before this migration get the default 24h grace period from now,
            // otherwise they'd never be picked up by RejectedSpotCleanupService.
            // 2 = SpotModerationStatus.Rejected.
            migrationBuilder.Sql(
                "UPDATE \"Spots\" SET \"ScheduledDeletionAt\" = now() + interval '24 hours' WHERE \"ModerationStatus\" = 2;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Spots");

            migrationBuilder.DropColumn(
                name: "ScheduledDeletionAt",
                table: "Spots");
        }
    }
}
