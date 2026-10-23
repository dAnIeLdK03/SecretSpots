using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Notifications;

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
        await AdminNotifier.AddForAllAdminsAsync(db, NotificationType.ReportSubmitted, relatedSpotId, null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }
}
