namespace SecretSpots.Features.Notifications;

public record NotificationsPageResponse(
    IReadOnlyList<NotificationResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
