using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretSpots.Features.Common.Persistence.Migrations
{
    // xmin is a Postgres system column that already exists on every table — EF Core's
    // scaffolding doesn't know that and generates an AddColumn/DropColumn pair that would fail
    // ("column name "xmin" conflicts with a system column name"). This migration only exists to
    // record the model change (User now uses xmin as its concurrency token); no DDL is needed.
    public partial class UseXminConcurrencyTokenForUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
