using Microsoft.Extensions.Localization;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Localization;

namespace SecretSpots.Features.Notifications;

internal static class NotificationResponseFactory
{
    public static NotificationResponse Create(Notification notification, IStringLocalizer<SharedResources> localizer)
    {
        string SpotMessage(string key) => localizer[key, notification.RelatedSpotName ?? string.Empty].Value;

        var message = notification.Type switch
        {
            NotificationType.CrystalsEarned =>
                localizer[NotificationsMessageKeys.CrystalsEarnedMessage, notification.CrystalsAwarded ?? 0].Value,
            NotificationType.NewSpotNearby =>
                localizer[NotificationsMessageKeys.NewSpotNearbyMessage].Value,
            NotificationType.NewCommentOnYourSpot =>
                localizer[NotificationsMessageKeys.NewCommentOnYourSpotMessage].Value,
            NotificationType.NewRatingOnYourSpot =>
                localizer[NotificationsMessageKeys.NewRatingOnYourSpotMessage].Value,
            NotificationType.YourContentRemoved =>
                localizer[NotificationsMessageKeys.YourContentRemovedMessage].Value,
            NotificationType.ReportSubmitted =>
                localizer[NotificationsMessageKeys.ReportSubmittedMessage].Value,
            NotificationType.NewVisitorOnYourSpot =>
                localizer[NotificationsMessageKeys.NewVisitorOnYourSpotMessage, notification.CrystalsAwarded ?? 0].Value,
            NotificationType.SpotSubmittedForReview => SpotMessage(NotificationsMessageKeys.SpotSubmittedForReviewMessage),
            NotificationType.SpotChangeRequestSubmitted => SpotMessage(NotificationsMessageKeys.SpotChangeRequestSubmittedMessage),
            NotificationType.SpotApproved => SpotMessage(NotificationsMessageKeys.SpotApprovedMessage),
            NotificationType.SpotRejected => SpotMessage(NotificationsMessageKeys.SpotRejectedMessage),
            NotificationType.SpotRemoved => SpotMessage(NotificationsMessageKeys.SpotRemovedMessage),
            NotificationType.SpotRestored => SpotMessage(NotificationsMessageKeys.SpotRestoredMessage),
            NotificationType.SpotEditApproved => SpotMessage(NotificationsMessageKeys.SpotEditApprovedMessage),
            NotificationType.SpotEditRejected => SpotMessage(NotificationsMessageKeys.SpotEditRejectedMessage),
            NotificationType.SpotDeletionApproved => SpotMessage(NotificationsMessageKeys.SpotDeletionApprovedMessage),
            NotificationType.SpotDeletionRejected => SpotMessage(NotificationsMessageKeys.SpotDeletionRejectedMessage),
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
