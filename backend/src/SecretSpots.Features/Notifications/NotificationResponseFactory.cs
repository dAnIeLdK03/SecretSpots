using Microsoft.Extensions.Localization;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Localization;

namespace SecretSpots.Features.Notifications;

internal static class NotificationResponseFactory
{
    public static NotificationResponse Create(Notification notification, IStringLocalizer<SharedResources> localizer)
    {
        var message = notification.Type switch
        {
            NotificationType.CrystalsEarned =>
                localizer[NotificationsMessageKeys.CrystalsEarnedMessage, notification.CrystalsAwarded ?? 0].Value,
            NotificationType.NewSpotNearby =>
                localizer[NotificationsMessageKeys.NewSpotNearbyMessage].Value,
            _ => throw new ArgumentOutOfRangeException(nameof(notification), notification.Type, null),
        };

        return new NotificationResponse(
            notification.Id,
            notification.Type,
            message,
            notification.RelatedSpotId,
            notification.IsRead,
            notification.CreatedAt);
    }
}
