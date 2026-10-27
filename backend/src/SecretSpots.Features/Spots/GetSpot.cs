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

            // A Rejected spot is hidden from everyone except its creator and admins — a direct
            // lookup by anyone else returns the same not-found response as a truly nonexistent
            // spot, so the response doesn't reveal that a rejected spot exists. UserIdOrNull is
            // checked first because Role throws for anonymous requests.
            var isAdmin = userContext.UserIdOrNull is not null && userContext.Role == UserRole.Admin;
            var isHiddenFromThisViewer = spot is null
                || (spot.ModerationStatus == SpotModerationStatus.Rejected
                    && spot.CreatedByUserId != userContext.UserIdOrNull
                    && !isAdmin);

            if (isHiddenFromThisViewer || spot is null)
            {
                return Result<SpotResponse>.Failure(Error.NotFound(SpotsMessageKeys.NotFound, localizer));
            }

            var creatorDisplayName = await db.Users.GetDisplayNameAsync(spot.CreatedByUserId, cancellationToken);

            return Result<SpotResponse>.Success(SpotResponse.From(spot, creatorDisplayName));
        }
    }
}
