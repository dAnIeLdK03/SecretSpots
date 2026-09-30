using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretSpots.Features.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCrystalWalletAndTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CrystalTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<int>(type: "integer", nullable: false),
                    RelatedCheckInId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedRedemptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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

            // Backfill a wallet for every existing user from their current CrystalBalance —
            // must run before that column is dropped below, or the balance is lost.
            migrationBuilder.Sql(
                """
                INSERT INTO "CrystalWallets" ("UserId", "Balance")
                SELECT "Id", "CrystalBalance" FROM "Users";
                """);

            migrationBuilder.DropColumn(
                name: "CrystalBalance",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CrystalBalance",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Users",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.Sql(
                """
                UPDATE "Users" u SET "CrystalBalance" = w."Balance"
                FROM "CrystalWallets" w WHERE w."UserId" = u."Id";
                """);

            migrationBuilder.DropTable(
                name: "CrystalTransactions");

            migrationBuilder.DropTable(
                name: "CrystalWallets");
        }
    }
}
