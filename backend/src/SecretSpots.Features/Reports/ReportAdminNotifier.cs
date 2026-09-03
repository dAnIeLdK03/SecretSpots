using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Notifications;
using WebPush;

namespace SecretSpots.Features.Reports;

// Shared by ReportSpot and ReportComment — pages every admin (in-app notification + push) when
// a new report comes in, so the queue at /admin/reports doesn't rely on someone checking it cold.
internal static class ReportAdminNotifier
{
    public static async Task NotifyAsync(
        IAppDbContext db,
        WebPushClient webPushClient,
        IOptions<WebPushOptions> webPushOptions,
        IStringLocalizer<SharedResources> localizer,
        ILogger logger,
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

        foreach (var notification in notifications)
        {
            await PushNotificationSender.SendAsync(
                db, webPushClient, webPushOptions, localizer, logger, notification, cancellationToken);
        }
    }
}
