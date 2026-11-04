using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Storage;

namespace SecretSpots.Features.Spots;

// The one place spot photos are deleted from storage: a deleted spot's photos, the photos an
// approved edit removes, and uploads from a change request that never made it onto the spot
// (rejected, cancelled, replaced by a newer one, or its spot deleted) — which would otherwise be
// orphaned forever.
public static class SpotPhotoCleanup
{
    // Best-effort — a storage hiccup shouldn't fail the request or deletion that triggered it.
    // Worst case an orphaned object lingers in storage; it doesn't block anything.
    public static async Task DeleteAsync(
        IPhotoStorage photoStorage,
        IEnumerable<string> photoUrls,
        Guid spotId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        foreach (var photoUrl in photoUrls)
        {
            try
            {
                await photoStorage.DeleteAsync(photoUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, SpotsLogMessages.SpotPhotoDeleteFailed, photoUrl, spotId);
            }
        }
    }

    // Deletes the candidates that aren't in photoUrlsStillInUse; null candidates means nothing to do.
    // A photo that's still some check-in's proof photo is never deleted either: an admin can add a
    // visitor's check-in photo to the spot (AddCheckInPhotoToSpot), and dropping it from the spot
    // again must leave the visitor's own check-in intact.
    public static async Task DeleteUnusedAsync(
        IAppDbContext db,
        IPhotoStorage photoStorage,
        IEnumerable<string>? candidatePhotoUrls,
        IEnumerable<string> photoUrlsStillInUse,
        Guid spotId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var unused = candidatePhotoUrls?.Except(photoUrlsStillInUse).ToList() ?? [];
        if (unused.Count == 0)
        {
            return;
        }

        var usedByCheckIns = await db.CheckIns
            .Where(c => unused.Contains(c.PhotoUrl))
            .Select(c => c.PhotoUrl)
            .ToListAsync(cancellationToken);

        await DeleteAsync(photoStorage, unused.Except(usedByCheckIns), spotId, logger, cancellationToken);
    }
}
