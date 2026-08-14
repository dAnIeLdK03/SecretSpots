namespace SecretSpots.Features.Common.Persistence;

// Log message templates only — operational/diagnostic text, not user-facing,
// so it stays a plain constant (no bg/en translation needed).
internal static class TokenCleanupLogMessages
{
    public const string TokensCleanedUp =
        "Token cleanup removed {RefreshTokenCount} expired refresh token(s) and {ResetTokenCount} expired password reset token(s).";
}
