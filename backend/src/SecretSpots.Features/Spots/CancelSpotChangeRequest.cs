using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Common.Storage;

namespace SecretSpots.Features.Spots;

public static class CancelSpotChangeRequest
{
    public record Command(Guid SpotId) : IRequest<Result<Unit>>;

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
            var owned = await SpotOwnership.LoadOwnedAsync(db, command.SpotId, userContext, localizer, cancellationToken);
            if (!owned.IsSuccess)
            {
                return Result<Unit>.Failure(owned.Error);
            }

            var spot = owned.Value;

            var request = await PendingSpotChangeRequests.FindAsync(db, spot.Id, cancellationToken);
            if (request is null)
            {
                return Result<Unit>.Failure(Error.NotFound(SpotsMessageKeys.ChangeRequestNotFound, localizer));
            }

            // Removed rather than given a "Cancelled" status — a withdrawn proposal has nothing
            // worth keeping, and the admin never saw it as final.
            db.SpotChangeRequests.Remove(request);
            await db.SaveChangesAsync(cancellationToken);

            await SpotChangeRequestPhotos.DeleteUnusedAsync(
                photoStorage, request.ProposedPhotoUrls, spot.PhotoUrls, spot.Id, logger, cancellationToken);

            logger.LogInformation(SpotsLogMessages.SpotChangeRequestCancelled, spot.Id, userContext.UserId);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
