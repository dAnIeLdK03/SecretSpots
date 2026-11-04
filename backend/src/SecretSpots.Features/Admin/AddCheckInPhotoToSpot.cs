using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Notifications;
using SecretSpots.Features.Spots;

namespace SecretSpots.Features.Admin;

// Adds a visitor's check-in photo to the spot's own photos. The file stays the visitor's (it lives
// under their key and the check-in still shows it), so later removing it from the spot never
// deletes it — see SpotPhotoCleanup.DeleteUnusedAsync. The spot's creator can drop it again
// through a normal edit; SubmitSpotEdit accepts photos that are already on the spot.
public static class AddCheckInPhotoToSpot
{
    public record Command(Guid CheckInId) : IRequest<Result<Unit>>;

    public class Handler(
        IAppDbContext db,
        IUserContext userContext,
        IStringLocalizer<SharedResources> localizer,
        ILogger<Handler> logger)
        : IRequestHandler<Command, Result<Unit>>
    {
        public async Task<Result<Unit>> Handle(Command command, CancellationToken cancellationToken)
        {
            var checkIn = await db.CheckIns.SingleOrDefaultAsync(c => c.Id == command.CheckInId, cancellationToken);
            if (checkIn is null)
            {
                return Result<Unit>.Failure(Error.NotFound(AdminMessageKeys.CheckInNotFound, localizer));
            }

            var spot = await db.Spots.SingleOrDefaultAsync(s => s.Id == checkIn.SpotId, cancellationToken);
            if (spot is null)
            {
                return Result<Unit>.Failure(Error.NotFound(AdminMessageKeys.SpotNotFound, localizer));
            }

            // Already there (e.g. a double click): nothing to add, and no second notification.
            if (spot.PhotoUrls.Contains(checkIn.PhotoUrl))
            {
                return Result<Unit>.Success(Unit.Value);
            }

            if (spot.PhotoUrls.Count >= CreateSpot.MaxPhotoCount)
            {
                return Result<Unit>.Failure(Error.BadRequest(AdminMessageKeys.SpotPhotoLimitReached, localizer));
            }

            spot.PhotoUrls = [.. spot.PhotoUrls, checkIn.PhotoUrl];
            db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = checkIn.UserId,
                Type = NotificationType.CheckInPhotoAddedToSpot,
                RelatedSpotId = spot.Id,
                RelatedSpotName = spot.Name,
            });
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(AdminLogMessages.CheckInPhotoAddedToSpot, checkIn.Id, spot.Id, userContext.UserId);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
