using Microsoft.Extensions.Logging;
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
    public static Task DeleteUnusedAsync(
        IPhotoStorage photoStorage,
        IEnumerable<string>? candidatePhotoUrls,
        IEnumerable<string> photoUrlsStillInUse,
        Guid spotId,
        ILogger logger,
        CancellationToken cancellationToken) =>
        candidatePhotoUrls is null
            ? Task.CompletedTask
            : DeleteAsync(photoStorage, candidatePhotoUrls.Except(photoUrlsStillInUse), spotId, logger, cancellationToken);
}
