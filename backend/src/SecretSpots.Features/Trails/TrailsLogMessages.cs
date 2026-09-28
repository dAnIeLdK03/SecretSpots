namespace SecretSpots.Features.Trails;

// Log message templates only — operational/diagnostic text, not user-facing,
// so it stays a plain constant (no bg/en translation needed, unlike TrailsMessageKeys).
internal static class TrailsLogMessages
{
    public const string TrailCreated = "Trail {TrailId} ({PointCount} points, {DistanceMeters}m) created by user {UserId}.";
    public const string TrailDeleted = "Trail {TrailId} deleted by user {UserId}.";
}
