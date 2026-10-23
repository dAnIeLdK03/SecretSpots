using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Spots;

// The creator's most recent change request for a spot — the spot page uses it to show
// "waiting for review" (Pending) or the admin's reason (Rejected).
public static class GetMySpotChangeRequest
{
    public record Query(Guid SpotId) : IRequest<Result<SpotChangeRequestResponse>>;

    public class Handler(IAppDbContext db, IUserContext userContext, IStringLocalizer<SharedResources> localizer)
        : IRequestHandler<Query, Result<SpotChangeRequestResponse>>
    {
        public async Task<Result<SpotChangeRequestResponse>> Handle(Query query, CancellationToken cancellationToken)
        {
            var spot = await db.Spots.SingleOrDefaultAsync(s => s.Id == query.SpotId, cancellationToken);
            if (spot is null)
            {
                return Result<SpotChangeRequestResponse>.Failure(Error.NotFound(SpotsMessageKeys.NotFound, localizer));
            }

            if (spot.CreatedByUserId != userContext.UserId)
            {
                return Result<SpotChangeRequestResponse>.Failure(Error.Forbidden(SpotsMessageKeys.NotYourSpot, localizer));
            }

            var request = await db.SpotChangeRequests
                .Where(r => r.SpotId == spot.Id)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            return request is null
                ? Result<SpotChangeRequestResponse>.Failure(Error.NotFound(SpotsMessageKeys.ChangeRequestNotFound, localizer))
                : Result<SpotChangeRequestResponse>.Success(SpotChangeRequestResponse.From(request));
        }
    }
}
