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

// The creator asks for their spot to be deleted; an admin decides (ApproveSpotChangeRequest
// runs SpotDeletionCleanup). Replaces any request still pending for the spot, including an edit.
public static class SubmitSpotDeletion
{
    public record Command(Guid SpotId) : IRequest<Result<SpotChangeRequestResponse>>;

    public class Handler(
        IAppDbContext db,
        IUserContext userContext,
        IPhotoStorage photoStorage,
        IStringLocalizer<SharedResources> localizer,
        ILogger<Handler> logger)
        : IRequestHandler<Command, Result<SpotChangeRequestResponse>>
    {
        public async Task<Result<SpotChangeRequestResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            var owned = await SpotOwnership.LoadOwnedAsync(db, command.SpotId, userContext, localizer, cancellationToken);
            if (!owned.IsSuccess)
            {
                return Result<SpotChangeRequestResponse>.Failure(owned.Error);
            }

            var spot = owned.Value;

            // A deletion proposes nothing beyond its type.
            var (request, replacedPhotoUrls) = await PendingSpotChangeRequests.SubmitAsync(
                db, spot, userContext.UserId, SpotChangeRequestType.Deletion, _ => { }, cancellationToken);

            await SpotPhotoCleanup.DeleteUnusedAsync(
                photoStorage, replacedPhotoUrls, spot.PhotoUrls, spot.Id, logger, cancellationToken);

            logger.LogInformation(SpotsLogMessages.SpotDeletionRequested, spot.Id, userContext.UserId);

            return Result<SpotChangeRequestResponse>.Success(SpotChangeRequestResponse.From(request));
        }
    }
}
