namespace SecretSpots.Api;

// Startup-time configuration guard messages — never shown to end users, just fail-fast diagnostics.
internal static class StartupMessages
{
    public const string MissingPostgresConnectionString = "Missing 'ConnectionStrings:Postgres' configuration.";
    public const string MissingJwtConfiguration = "Missing or incomplete 'Jwt' configuration (Secret is required).";
    public const string WeakJwtSecret = "'Jwt:Secret' is too short — must be at least 32 bytes (256 bits) to safely key HMAC-SHA256.";
}
