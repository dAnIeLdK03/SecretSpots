using Microsoft.Extensions.Logging;
using SecretSpots.Features.Common.Storage;

namespace SecretSpots.Features.Spots;

// Photos uploaded for an Edit request live in R2 before anyone approves them. Whenever a proposal
// is dropped (rejected, cancelled, replaced by a newer one, or its spot deleted), the uploads
// that never made it onto the spot have to be cleaned up here, or they'd be orphaned forever.
public static class SpotChangeRequestPhotos
{
    public static async Task DeleteUnusedAsync(
        IPhotoStorage photoStorage,
        IEnumerable<string>? proposedPhotoUrls,
        IEnumerable<string> photoUrlsStillInUse,
        Guid spotId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (proposedPhotoUrls is null)
        {
            return;
        }

        // Best-effort, same as SpotDeletionCleanup — a storage hiccup shouldn't fail the request.
        foreach (var photoUrl in proposedPhotoUrls.Except(photoUrlsStillInUse))
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
}
