using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Common.Storage;

namespace SecretSpots.Features.Trails;

public static class DeleteTrail
{
    public record Command(Guid TrailId) : IRequest<Result<Unit>>;

    public class Handler(
        IAppDbContext db,
        IUserContext userContext,
        IPhotoStorage photoStorage,
        IStringLocalizer<SharedResources> localizer,
        ILogger<Handler> logger)
        : IRequestHandler<Command, Result<Unit>>
    {
        public async Task<Result<Unit>> Handle(Command command, CancellationToken cancellationToken)
        {
            var trail = await db.Trails.SingleOrDefaultAsync(t => t.Id == command.TrailId, cancellationToken);
            if (trail is null)
            {
                return Result<Unit>.Failure(new Error(
                    TrailsMessageKeys.NotFound,
                    localizer[TrailsMessageKeys.NotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            if (trail.CreatedByUserId != userContext.UserId)
            {
                return Result<Unit>.Failure(new Error(
                    TrailsMessageKeys.NotYourTrail,
                    localizer[TrailsMessageKeys.NotYourTrail].Value,
                    StatusCodes.Status403Forbidden));
            }

            db.Trails.Remove(trail);
            await db.SaveChangesAsync(cancellationToken);

            // Best-effort — same as SpotDeletionCleanup/UpdateSpot: a storage hiccup shouldn't
            // block the delete from completing, worst case an orphaned object lingers in R2.
            foreach (var photoUrl in trail.PhotoUrls)
            {
                try
                {
                    await photoStorage.DeleteAsync(photoUrl, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to delete photo {PhotoUrl} for deleted trail {TrailId}.", photoUrl, trail.Id);
                }
            }

            logger.LogInformation(TrailsLogMessages.TrailDeleted, trail.Id, userContext.UserId);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
