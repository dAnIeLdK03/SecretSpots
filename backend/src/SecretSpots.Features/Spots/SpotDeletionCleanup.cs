using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Storage;

namespace SecretSpots.Features.Spots;

// Shared by DeleteSpot and DeleteAccount (deleting an account deletes every spot it created) —
// keeps the cascade logic in one place so the two can't drift apart.
public static class SpotDeletionCleanup
{
    public static async Task DeleteAsync(
        IAppDbContext db, IPhotoStorage photoStorage, Spot spot, ILogger logger, CancellationToken cancellationToken)
    {
        db.Spots.Remove(spot);
        await db.SaveChangesAsync(cancellationToken);

        await db.Notifications
            .Where(n => n.RelatedSpotId == spot.Id)
            .ExecuteUpdateAsync(n => n.SetProperty(x => x.RelatedSpotId, (Guid?)null), cancellationToken);

        // Captured before the comments themselves are deleted below — needed to resolve reports
        // filed against them (Report.ContentId has no FK to Comment, so nothing else would find
        // them once the rows are gone).
        var commentIds = await db.Comments
            .Where(c => c.SpotId == spot.Id)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        // Otherwise this spot (or a comment on it) escapes moderation entirely: unlike
        // DeleteReportedContent's admin-initiated deletion, nothing here previously touched
        // Reports, so a reported user could delete their way out of an open report and leave it
        // stuck unresolved in the admin queue, pointing at content that no longer exists.
        await db.Reports
            .Where(r =>
                (r.ContentType == ReportedContentType.Spot && r.ContentId == spot.Id)
                || (r.ContentType == ReportedContentType.Comment && commentIds.Contains(r.ContentId)))
            .Where(r => r.ResolvedAt == null)
            .ExecuteUpdateAsync(r => r
                .SetProperty(x => x.ResolvedAt, DateTimeOffset.UtcNow)
                .SetProperty(x => x.ResolutionAction, ReportResolutionAction.ContentDeletedByAuthor), cancellationToken);

        // Comment/Rating/SavedSpot/CheckIn have a required (non-nullable) SpotId, so unlike
        // Notification.RelatedSpotId there's no "clear the link" option — the rows themselves
        // are now meaningless and would otherwise sit as permanent DB bloat.
        await db.Comments.Where(c => c.SpotId == spot.Id).ExecuteDeleteAsync(cancellationToken);
        await db.Ratings.Where(r => r.SpotId == spot.Id).ExecuteDeleteAsync(cancellationToken);
        await db.SavedSpots.Where(s => s.SpotId == spot.Id).ExecuteDeleteAsync(cancellationToken);
        await db.CheckIns.Where(c => c.SpotId == spot.Id).ExecuteDeleteAsync(cancellationToken);

        // Best-effort — a storage hiccup shouldn't stop the spot/account deletion. Worst case an
        // orphaned object lingers in R2; it doesn't block anything.
        foreach (var photoUrl in spot.PhotoUrls)
        {
            try
            {
                await photoStorage.DeleteAsync(photoUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, SpotsLogMessages.SpotPhotoDeleteFailed, photoUrl, spot.Id);
            }
        }
    }
}
