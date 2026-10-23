using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;

namespace SecretSpots.Features.Notifications;

// Tells a spot's creator about a moderation decision on it. Only adds the row — the caller saves
// it together with the decision itself.
public static class SpotOwnerNotifier
{
    // relatedSpotId is null when the spot no longer exists (an approved deletion).
    public static void Add(IAppDbContext db, NotificationType type, Spot spot, Guid? relatedSpotId)
    {
        db.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = spot.CreatedByUserId,
            Type = type,
            RelatedSpotId = relatedSpotId,
            RelatedSpotName = spot.Name,
        });
    }
}
