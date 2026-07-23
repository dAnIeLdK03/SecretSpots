namespace SecretSpots.Features.Spots;

// Log message templates only — operational/diagnostic text, not user-facing,
// so it stays a plain constant (no bg/en translation needed, unlike SpotsMessageKeys).
internal static class SpotsLogMessages
{
    public const string SpotCreated = "Spot {SpotId} ({Category}) created by user {UserId}.";
    public const string SpotUpdated = "Spot {SpotId} updated by user {UserId}.";
    public const string SpotDeleted = "Spot {SpotId} deleted by user {UserId}.";
    public const string NearbyUsersNotified = "Notified {NotifiedCount} nearby user(s) about new spot {SpotId}.";
}
