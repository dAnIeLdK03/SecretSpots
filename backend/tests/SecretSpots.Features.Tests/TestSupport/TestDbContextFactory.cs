using Microsoft.EntityFrameworkCore;
using SecretSpots.Features.Common.Persistence;

namespace SecretSpots.Features.Tests.TestSupport;

// Hits the real local Postgres+PostGIS container (separate "secretspots_test" database) —
// Register.Handler uses Database.BeginTransactionAsync, a relational-only API the EF Core
// InMemory provider does not support, so a real relational engine is required here.
//
// The connection string comes from an env var rather than a literal, so a local-only dev
// credential never sits in source control (GitGuardian flagged the previous literal in review).
internal static class TestDbContextFactory
{
    private const string ConnectionStringEnvVar = "SECRETSPOTS_TEST_CONNECTION_STRING";
    private const string MissingConnectionStringMessage =
        "Missing '" + ConnectionStringEnvVar + "' environment variable. See CONTRIBUTING.md for local test setup.";

    public static AppDbContext Create()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvVar)
            ?? throw new InvalidOperationException(MissingConnectionStringMessage);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.UseNetTopologySuite())
            .Options;

        var context = new AppDbContext(options);
        context.Database.Migrate();
        return context;
    }
}
