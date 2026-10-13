using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretSpots.Features.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE \"Users\" SET \"Role\" = 1 WHERE \"IsAdmin\" = true;");

            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE \"Users\" SET \"IsAdmin\" = true WHERE \"Role\" = 1;");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");
        }
    }
}
