namespace SecretSpots.Features.Auth;

// Log message templates only — operational/diagnostic text, not user-facing,
// so it stays a plain constant (no bg/en translation needed, unlike AuthMessageKeys).
internal static class AuthLogMessages
{
    public const string UserRegistered = "User {UserId} registered with email {Email}.";
    public const string FailedLoginAttempt = "Failed login attempt for email {Email}.";
    public const string UserLoggedIn = "User {UserId} logged in successfully.";
    public const string UserProfileRetrieved = "User {UserId} retrieved their profile.";
}
