namespace SecretSpots.Features.Spots;

// Log message templates only — operational/diagnostic text, not user-facing,
// so it stays a plain constant (no bg/en translation needed, unlike SpotsMessageKeys).
internal static class SpotsLogMessages
{
    public const string SpotCreated = "Spot {SpotId} ({Category}) created by user {UserId}.";
    public const string SpotEditRequested = "Edit requested for spot {SpotId} by user {UserId}.";
    public const string SpotDeletionRequested = "Deletion requested for spot {SpotId} by user {UserId}.";
    public const string SpotChangeRequestCancelled = "Pending change request for spot {SpotId} cancelled by user {UserId}.";
    public const string SpotPhotoDeleteFailed = "Failed to delete photo {PhotoUrl} for deleted spot {SpotId}.";
    public const string RejectedSpotsSoftDeleted = "Soft-deleted {DeletedCount} rejected spot(s) whose grace period had passed.";
    public const string NearbyUsersNotified = "Notified {NotifiedCount} nearby user(s) about new spot {SpotId}.";
}
