using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretSpots.Features.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSpotPhotoUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add + backfill + drop, not EF's default drop-then-add, so existing spots keep
            // their photo instead of losing it.
            migrationBuilder.AddColumn<List<string>>(
                name: "PhotoUrls",
                table: "Spots",
                type: "text[]",
                nullable: false,
                defaultValue: new List<string>());

            migrationBuilder.Sql(@"UPDATE ""Spots"" SET ""PhotoUrls"" = ARRAY[""PhotoUrl""];");

            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "Spots");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "Spots",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"UPDATE ""Spots"" SET ""PhotoUrl"" = ""PhotoUrls""[1];");

            migrationBuilder.DropColumn(
                name: "PhotoUrls",
                table: "Spots");
        }
    }
}
