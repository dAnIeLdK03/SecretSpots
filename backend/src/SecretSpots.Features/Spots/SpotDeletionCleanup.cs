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
