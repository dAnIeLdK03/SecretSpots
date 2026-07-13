using SecretSpots.Domain;

namespace SecretSpots.Features.Notifications;

public record NotificationResponse(
    Guid Id,
    NotificationType Type,
    string Message,
    Guid? RelatedSpotId,
    bool IsRead,
    DateTimeOffset CreatedAt);
