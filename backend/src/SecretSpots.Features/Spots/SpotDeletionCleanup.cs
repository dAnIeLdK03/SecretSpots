using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Storage;

namespace SecretSpots.Features.Spots;

// Shared by ApproveSpotChangeRequest (an approved deletion request), DeleteAccount (deleting an account deletes every
// spot it created) and DeleteReportedContent (an admin removing a reported spot) — keeps the cascade logic in one
// place so they can't drift apart.
public static class SpotDeletionCleanup
{
    // Open reports on the spot/its comments are resolved as deleted by the author unless the caller is an admin
    // acting on a report, who passes ContentDeleted and their own id instead.
    public static async Task DeleteAsync(
        IAppDbContext db,
        IPhotoStorage photoStorage,
        Spot spot,
        ILogger logger,
        CancellationToken cancellationToken,
        ReportResolutionAction reportResolution = ReportResolutionAction.ContentDeletedByAuthor,
        Guid? resolvedByUserId = null)
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
                .SetProperty(x => x.ResolvedByUserId, resolvedByUserId)
                .SetProperty(x => x.ResolutionAction, reportResolution), cancellationToken);

        // Comment/Rating/SavedSpot/CheckIn have a required (non-nullable) SpotId, so unlike
        // Notification.RelatedSpotId there's no "clear the link" option — the rows themselves
        // are now meaningless and would otherwise sit as permanent DB bloat.
        await db.Comments.Where(c => c.SpotId == spot.Id).ExecuteDeleteAsync(cancellationToken);
        await db.Ratings.Where(r => r.SpotId == spot.Id).ExecuteDeleteAsync(cancellationToken);
        await db.SavedSpots.Where(s => s.SpotId == spot.Id).ExecuteDeleteAsync(cancellationToken);
        await db.CheckIns.Where(c => c.SpotId == spot.Id).ExecuteDeleteAsync(cancellationToken);

        // Captured before the rows go — a still-pending edit may have uploaded photos that
        // aren't on the spot, and nothing else would ever clean those up.
        var proposedPhotoUrls = await db.SpotChangeRequests
            .Where(r => r.SpotId == spot.Id && r.Status == SpotChangeRequestStatus.Pending && r.ProposedPhotoUrls != null)
            .Select(r => r.ProposedPhotoUrls!)
            .ToListAsync(cancellationToken);
        await db.SpotChangeRequests.Where(r => r.SpotId == spot.Id).ExecuteDeleteAsync(cancellationToken);

        // The spot's own photos plus any pending uploads — Distinct because a proposal usually
        // keeps some of the spot's current photos.
        await SpotPhotoCleanup.DeleteAsync(
            photoStorage,
            spot.PhotoUrls.Concat(proposedPhotoUrls.SelectMany(urls => urls)).Distinct(),
            spot.Id,
            logger,
            cancellationToken);
    }
}
