using Microsoft.EntityFrameworkCore;
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
            var spot = await db.Spots.SingleOrDefaultAsync(s => s.Id == command.SpotId, cancellationToken);
            if (spot is null)
            {
                return Result<SpotChangeRequestResponse>.Failure(Error.NotFound(SpotsMessageKeys.NotFound, localizer));
            }

            if (spot.CreatedByUserId != userContext.UserId)
            {
                return Result<SpotChangeRequestResponse>.Failure(Error.Forbidden(SpotsMessageKeys.NotYourSpot, localizer));
            }

            var request = await db.SpotChangeRequests.SingleOrDefaultAsync(
                r => r.SpotId == spot.Id && r.Status == SpotChangeRequestStatus.Pending, cancellationToken);

            var replacedPhotoUrls = request?.ProposedPhotoUrls;

            if (request is null)
            {
                request = new SpotChangeRequest
                {
                    Id = Guid.NewGuid(),
                    SpotId = spot.Id,
                    RequestedByUserId = userContext.UserId,
                };
                db.SpotChangeRequests.Add(request);
            }
            else
            {
                request.CreatedAt = DateTimeOffset.UtcNow;
            }

            request.Type = SpotChangeRequestType.Deletion;
            request.ProposedName = null;
            request.ProposedDescription = null;
            request.ProposedCategory = null;
            request.ProposedPhotoUrls = null;

            await db.SaveChangesAsync(cancellationToken);

            await SpotChangeRequestPhotos.DeleteUnusedAsync(
                photoStorage, replacedPhotoUrls, spot.PhotoUrls, spot.Id, logger, cancellationToken);

            logger.LogInformation(SpotsLogMessages.SpotDeletionRequested, spot.Id, userContext.UserId);

            return Result<SpotChangeRequestResponse>.Success(SpotChangeRequestResponse.From(request));
        }
    }
}
