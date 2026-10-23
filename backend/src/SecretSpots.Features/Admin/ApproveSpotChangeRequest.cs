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
using SecretSpots.Features.Spots;

namespace SecretSpots.Features.Admin;

public static class ApproveSpotChangeRequest
{
    public record Command(Guid RequestId) : IRequest<Result<Unit>>;

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
            var request = await db.SpotChangeRequests.SingleOrDefaultAsync(
                r => r.Id == command.RequestId && r.Status == SpotChangeRequestStatus.Pending, cancellationToken);
            var spot = request is null
                ? null
                : await db.Spots.SingleOrDefaultAsync(s => s.Id == request.SpotId, cancellationToken);

            if (request is null || spot is null)
            {
                return Result<Unit>.Failure(Error.NotFound(AdminMessageKeys.ChangeRequestNotFound, localizer));
            }

            if (request.Type == SpotChangeRequestType.Deletion)
            {
                // SpotDeletionCleanup also deletes this request row along with everything else
                // attached to the spot, so there's no status to update first.
                await SpotDeletionCleanup.DeleteAsync(db, photoStorage, spot, logger, cancellationToken);
                logger.LogInformation(AdminLogMessages.SpotChangeRequestApproved, request.Id, request.Type, spot.Id, userContext.UserId);
                return Result<Unit>.Success(Unit.Value);
            }

            var removedPhotoUrls = spot.PhotoUrls.Except(request.ProposedPhotoUrls!).ToList();

            spot.Name = request.ProposedName!;
            spot.Description = request.ProposedDescription!;
            spot.Category = request.ProposedCategory!.Value;
            spot.PhotoUrls = request.ProposedPhotoUrls!.ToList();

            request.Status = SpotChangeRequestStatus.Approved;
            request.ReviewedByUserId = userContext.UserId;
            request.ReviewedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(cancellationToken);

            await SpotChangeRequestPhotos.DeleteUnusedAsync(
                photoStorage, removedPhotoUrls, spot.PhotoUrls, spot.Id, logger, cancellationToken);

            logger.LogInformation(AdminLogMessages.SpotChangeRequestApproved, request.Id, request.Type, spot.Id, userContext.UserId);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
