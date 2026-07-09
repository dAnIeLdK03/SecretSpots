namespace SecretSpots.Api;

// Startup-time configuration guard messages — never shown to end users, just fail-fast diagnostics.
internal static class StartupMessages
{
    public const string MissingPostgresConnectionString = "Missing 'ConnectionStrings:Postgres' configuration.";
    public const string MissingJwtConfiguration = "Missing or incomplete 'Jwt' configuration (Secret is required).";
}
