namespace SecretSpots.Features.Photos;

// Log message templates only — operational/diagnostic text, not user-facing,
// so it stays a plain constant (no bg/en translation needed, unlike PhotoMessageKeys).
internal static class PhotoLogMessages
{
    public const string PhotoUploaded = "Photo {Key} uploaded by user {UserId}.";
}
