using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretSpots.Features.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCrystals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Type 0 was CrystalsEarned — the enum member is gone, so these rows could no longer
            // be read back.
            migrationBuilder.Sql("""DELETE FROM "Notifications" WHERE "Type" = 0;""");

            migrationBuilder.DropTable(
                name: "CrystalTransactions");

            migrationBuilder.DropTable(
                name: "CrystalWallets");

            migrationBuilder.DropColumn(
                name: "CrystalsAwarded",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "CrystalsAwarded",
                table: "CheckIns");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CrystalsAwarded",
                table: "Notifications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CrystalsAwarded",
                table: "CheckIns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CrystalTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<int>(type: "integer", nullable: false),
                    RelatedCheckInId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedRedemptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrystalTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrystalWallets",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Balance = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrystalWallets", x => x.UserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrystalTransactions_UserId_CreatedAt",
                table: "CrystalTransactions",
                columns: new[] { "UserId", "CreatedAt" });
        }
    }
}
