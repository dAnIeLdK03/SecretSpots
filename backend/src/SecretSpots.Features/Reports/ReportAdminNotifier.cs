using Microsoft.EntityFrameworkCore;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;

namespace SecretSpots.Features.Reports;

// Shared by ReportSpot and ReportComment — pages every admin with an in-app notification when a
// new report comes in, so the queue at /admin/reports doesn't rely on someone checking it cold.
internal static class ReportAdminNotifier
{
    public static async Task NotifyAsync(
        IAppDbContext db,
        Guid relatedSpotId,
        CancellationToken cancellationToken)
    {
        var adminIds = await db.Users
            .Where(u => u.IsAdmin)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (adminIds.Count == 0)
        {
            return;
        }

        var notifications = adminIds
            .Select(adminId => new Notification
            {
                Id = Guid.NewGuid(),
                UserId = adminId,
                Type = NotificationType.ReportSubmitted,
                RelatedSpotId = relatedSpotId,
            })
            .ToList();

        db.Notifications.AddRange(notifications);
        await db.SaveChangesAsync(cancellationToken);
    }
}
