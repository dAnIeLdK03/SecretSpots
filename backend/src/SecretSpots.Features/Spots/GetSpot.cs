using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Spots;

public static class GetSpot
{
    public record Query(Guid SpotId) : IRequest<Result<SpotResponse>>;

    public class Handler(IAppDbContext db, IUserContext userContext, IStringLocalizer<SharedResources> localizer)
        : IRequestHandler<Query, Result<SpotResponse>>
    {
        public async Task<Result<SpotResponse>> Handle(Query query, CancellationToken cancellationToken)
        {
            var spot = await db.Spots.SingleOrDefaultAsync(s => s.Id == query.SpotId, cancellationToken);

            // A Rejected spot is hidden from everyone except its creator — a direct lookup by
            // anyone else returns the same not-found response as a truly nonexistent spot, so
            // the response doesn't reveal that a rejected spot exists.
            var isHiddenFromThisViewer = spot is null
                || (spot.ModerationStatus == SpotModerationStatus.Rejected && spot.CreatedByUserId != userContext.UserIdOrNull);

            if (isHiddenFromThisViewer || spot is null)
            {
                return Result<SpotResponse>.Failure(Error.NotFound(SpotsMessageKeys.NotFound, localizer));
            }

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
