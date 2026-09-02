namespace SecretSpots.Features.Notifications;

// Keys into the shared SharedResources.resx / SharedResources.bg.resx pair
// (Common/Localization) — Notifications keeps only the constants, not the translations.
public static class NotificationsMessageKeys
{
    public const string CrystalsEarnedMessage = "Notifications.CrystalsEarnedMessage";
    public const string NewSpotNearbyMessage = "Notifications.NewSpotNearbyMessage";
    public const string NewCommentOnYourSpotMessage = "Notifications.NewCommentOnYourSpotMessage";
    public const string NewRatingOnYourSpotMessage = "Notifications.NewRatingOnYourSpotMessage";
    public const string NotFound = "Notifications.NotFound";
    public const string PageOutOfRange = "Notifications.PageOutOfRange";
    public const string PageSizeOutOfRange = "Notifications.PageSizeOutOfRange";
    public const string PushEndpointRequired = "Notifications.PushEndpointRequired";
    public const string PushKeysRequired = "Notifications.PushKeysRequired";
}
