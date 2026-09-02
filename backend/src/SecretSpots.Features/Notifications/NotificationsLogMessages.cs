namespace SecretSpots.Features.Notifications;

internal static class NotificationsLogMessages
{
    public const string NotificationMarkedAsRead = "Notification {NotificationId} marked as read by user {UserId}.";
    public const string AllNotificationsMarkedAsRead = "{Count} notification(s) marked as read by user {UserId}.";
    public const string PushSendFailed = "Failed to send a push notification via subscription {PushSubscriptionId} for user {UserId}.";
}
