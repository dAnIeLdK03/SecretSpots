namespace SecretSpots.Features.Common.Email;

// Log message templates only — operational/diagnostic text, not user-facing,
// so it stays a plain constant (no bg/en translation needed).
internal static class EmailLogMessages
{
    public const string QueueClosed = "Email to {Recipient} not queued: the app is shutting down.";
    public const string SendAttemptFailed = "Sending email to {Recipient} failed (attempt {Attempt} of {MaxAttempts}); retrying in {Delay}.";
    public const string SendFailed = "Giving up on email to {Recipient} after {MaxAttempts} attempts.";
}
