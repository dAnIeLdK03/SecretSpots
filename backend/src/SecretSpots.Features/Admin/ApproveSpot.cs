using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Spots;
using SecretSpots.Features.Notifications;

namespace SecretSpots.Features.Admin;

public static class ApproveSpot
{
    public record Command(Guid SpotId) : IRequest<Result<SpotResponse>>;

    public class Handler(IAppDbContext db, IUserContext userContext, IStringLocalizer<SharedResources> localizer, ILogger<Handler> logger)
        : IRequestHandler<Command, Result<SpotResponse>>
    {
        public async Task<Result<SpotResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            var spot = await db.Spots.SingleOrDefaultAsync(s => s.Id == command.SpotId, cancellationToken);
            if (spot is null)
            {
                return Result<SpotResponse>.Failure(Error.NotFound(AdminMessageKeys.SpotNotFound, localizer));
            }

            // Approving a Rejected spot undoes the rejection — the creator hears it as a restore.
            var notificationType = spot.ModerationStatus == SpotModerationStatus.Rejected
                ? NotificationType.SpotRestored
                : NotificationType.SpotApproved;
            var statusChanged = spot.ModerationStatus != SpotModerationStatus.Approved;

            spot.ModerationStatus = SpotModerationStatus.Approved;

            // Clear any stale rejection reason — e.g. the creator fixed the spot off-platform
            // and an admin reconsidered. A rejection reason shouldn't linger on an Approved spot.
            spot.RejectionReason = null;
            spot.ScheduledDeletionAt = null;

            if (statusChanged)
            {
                SpotOwnerNotifier.Add(db, notificationType, spot, spot.Id);
            }

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(AdminLogMessages.SpotApproved, spot.Id, userContext.UserId);

            var creatorDisplayName = await db.Users
                .Where(u => u.Id == spot.CreatedByUserId)
                .Select(u => u.DisplayName)
                .SingleOrDefaultAsync(cancellationToken) ?? string.Empty;

            return Result<SpotResponse>.Success(new SpotResponse(
                spot.Id,
                spot.Name,
                spot.Description,
                spot.Category,
                spot.PhotoUrls,
                spot.Location.Y,
                spot.Location.X,
                spot.CreatedByUserId,
                creatorDisplayName,
                spot.AverageRating,
                spot.RatingsCount,
                spot.CreatedAt,
                spot.ModerationStatus,
                spot.RejectionReason,
                spot.ScheduledDeletionAt));
        }
    }
}
