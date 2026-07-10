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

    // xUnit runs test classes in parallel by default; without this, concurrent Create() calls
    // race to apply the same pending migration against the shared real Postgres test database
    // ("column already exists"). Migrate() only ever needs to run once per test process.
    private static readonly object MigrationLock = new();
    private static bool _migrated;

    public static AppDbContext Create()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvVar)
            ?? throw new InvalidOperationException(MissingConnectionStringMessage);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.UseNetTopologySuite())
            .Options;

        var context = new AppDbContext(options);

        if (!_migrated)
        {
            lock (MigrationLock)
            {
                if (!_migrated)
                {
                    context.Database.Migrate();
                    _migrated = true;
                }
            }
        }

        return context;
    }
}
