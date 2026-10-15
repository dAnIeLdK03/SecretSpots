namespace SecretSpots.Features.Admin;

// Log message templates only — operational/diagnostic text, not user-facing,
// so it stays a plain constant (no bg/en translation needed, unlike AdminMessageKeys).
internal static class AdminLogMessages
{
    public const string BootstrapAdminsPromoted = "Promoted {Count} user(s) to Admin via AdminBootstrap config.";
    public const string UserRoleChanged = "User {UserId} role changed to {Role} by admin {AdminUserId}.";
}
