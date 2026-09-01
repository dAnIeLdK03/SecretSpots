namespace SecretSpots.Features.Auth;

// Log message templates only — operational/diagnostic text, not user-facing,
// so it stays a plain constant (no bg/en translation needed, unlike AuthMessageKeys).
internal static class AuthLogMessages
{
    public const string UserRegistered = "User {UserId} registered with email {Email}.";
    public const string FailedLoginAttempt = "Failed login attempt for email {Email}.";
    public const string UserLoggedIn = "User {UserId} logged in successfully.";
    public const string UserProfileRetrieved = "User {UserId} retrieved their profile.";
    public const string ExternalAuthDenied = "External auth with {Provider} was denied or cancelled by the user.";
    public const string ExternalAuthInvalidState = "External auth callback for {Provider} had an invalid or expired state.";
    public const string ExternalAuthProviderExchangeFailed = "External auth code exchange with {Provider} failed.";
    public const string ExternalAuthCompleted = "User {UserId} completed external auth with {Provider}.";
    public const string PasswordResetRequested = "User {UserId} requested a password reset.";
    public const string PasswordResetRequestedForUnknownEmail =
        "Password reset requested for an email with no password-based account: {Email}.";
    public const string PasswordResetCompleted = "User {UserId} completed a password reset.";
    public const string RefreshTokenReuseDetected =
        "Refresh token reuse detected for user {UserId} — an already-revoked token was presented again. Revoking all active sessions.";
    public const string AccountDeleted = "User {UserId} deleted their account.";
    public const string EmailVerificationSendFailedAfterRegister =
        "Failed to send the verification email for newly-registered user {UserId}. Registration still succeeded.";
    public const string EmailVerificationSent = "User {UserId} requested a new verification email.";
    public const string EmailVerificationCompleted = "User {UserId} verified their email.";
}
