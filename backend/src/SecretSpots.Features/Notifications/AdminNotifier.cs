using Microsoft.EntityFrameworkCore;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;

namespace SecretSpots.Features.Notifications;

// Pages every admin with an in-app notification about something waiting in an admin queue
// (reports, new spots, spot change requests). Only adds the rows — the caller saves them
// together with the change that triggered them.
public static class AdminNotifier
{
    public static async Task AddForAllAdminsAsync(
        IAppDbContext db,
        NotificationType type,
        Guid relatedSpotId,
        string? relatedSpotName,
        CancellationToken cancellationToken)
    {
        var adminIds = await db.Users
            .Where(u => u.Role == UserRole.Admin)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        db.Notifications.AddRange(adminIds.Select(adminId => new Notification
        {
            Id = Guid.NewGuid(),
            UserId = adminId,
            Type = type,
            RelatedSpotId = relatedSpotId,
            RelatedSpotName = relatedSpotName,
        }));
    }
}
