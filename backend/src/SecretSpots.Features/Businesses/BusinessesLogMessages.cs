namespace SecretSpots.Features.Businesses;

internal static class BusinessesLogMessages
{
    public const string BusinessCreated = "Business {BusinessId} created by user {UserId}.";
    public const string BusinessDeleted = "Business {BusinessId} deleted by user {UserId}.";
    public const string BusinessActiveStateChanged = "Business {BusinessId} active state set to {IsActive} by user {UserId}.";
}
