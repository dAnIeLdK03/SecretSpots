using Microsoft.EntityFrameworkCore;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Notifications;

namespace SecretSpots.Features.Spots;

// The rules for a spot's one pending change request, shared by SubmitSpotEdit and
// SubmitSpotDeletion: a new submission replaces the pending one (and goes to the back of the
// review queue) instead of queueing behind it, and admins are paged only for a brand-new request.
public static class PendingSpotChangeRequests
{
    public static Task<SpotChangeRequest?> FindAsync(IAppDbContext db, Guid spotId, CancellationToken cancellationToken) =>
        db.SpotChangeRequests.SingleOrDefaultAsync(
            r => r.SpotId == spotId && r.Status == SpotChangeRequestStatus.Pending, cancellationToken);

    // Saves the submission as the spot's pending request. setProposal fills in the type-specific
    // Proposed* fields (they're cleared first, so a deletion replacing an edit carries none over).
    // Returns the replaced proposal's photo uploads, which the caller deletes once they're unused.
    public static async Task<(SpotChangeRequest Request, IReadOnlyList<string>? ReplacedPhotoUrls)> SubmitAsync(
        IAppDbContext db,
        Spot spot,
        Guid requestedByUserId,
        SpotChangeRequestType type,
        Action<SpotChangeRequest> setProposal,
        CancellationToken cancellationToken)
    {
        var pending = await FindAsync(db, spot.Id, cancellationToken);

        if (pending is null)
        {
            var created = new SpotChangeRequest
            {
                Id = Guid.NewGuid(),
                SpotId = spot.Id,
                RequestedByUserId = requestedByUserId,
            };
            Apply(created, type, setProposal);
            db.SpotChangeRequests.Add(created);
            var adminNotifications = await AdminNotifier.AddForAllAdminsAsync(
                db, NotificationType.SpotChangeRequestSubmitted, spot.Id, spot.Name, cancellationToken);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return (created, null);
            }
            catch (DbUpdateException)
            {
                // A concurrent submission for the same spot (double-click, two tabs) created the
                // pending request between the lookup above and this write — the filtered unique
                // index on SpotId caught it. Drop this attempt's still-Added rows (same Remove()
                // pattern as RateSpot, since IAppDbContext doesn't expose the ChangeTracker) and
                // replace the winner's request instead, as if this submission had come second.
                db.SpotChangeRequests.Remove(created);
                foreach (var notification in adminNotifications)
                {
                    db.Notifications.Remove(notification);
                }

                pending = await FindAsync(db, spot.Id, cancellationToken);
                if (pending is null)
                {
                    // Not the race above — a genuine failure.
                    throw;
                }
            }
        }

        var replacedPhotoUrls = pending.ProposedPhotoUrls;
        pending.CreatedAt = DateTimeOffset.UtcNow;
        Apply(pending, type, setProposal);
        await db.SaveChangesAsync(cancellationToken);

        return (pending, replacedPhotoUrls);
    }

    private static void Apply(SpotChangeRequest request, SpotChangeRequestType type, Action<SpotChangeRequest> setProposal)
    {
        request.Type = type;
        request.ProposedName = null;
        request.ProposedDescription = null;
        request.ProposedCategory = null;
        request.ProposedPhotoUrls = null;
        setProposal(request);
    }
}
