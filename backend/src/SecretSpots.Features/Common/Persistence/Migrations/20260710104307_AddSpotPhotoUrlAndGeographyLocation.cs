using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace SecretSpots.Features.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSpotPhotoUrlAndGeographyLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Point>(
                name: "Location",
                table: "Spots",
                type: "geography (Point, 4326)",
                nullable: false,
                oldClrType: typeof(Point),
                oldType: "geometry");

            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "Spots",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Spots_Location",
                table: "Spots",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "GIST");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Spots_Location",
                table: "Spots");

            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "Spots");

            migrationBuilder.AlterColumn<Point>(
                name: "Location",
                table: "Spots",
                type: "geometry",
                nullable: false,
                oldClrType: typeof(Point),
                oldType: "geography (Point, 4326)");
        }
    }
}
