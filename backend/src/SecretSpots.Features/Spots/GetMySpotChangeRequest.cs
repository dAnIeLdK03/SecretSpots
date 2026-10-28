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
            var owned = await SpotOwnership.LoadOwnedAsync(db, query.SpotId, userContext, localizer, cancellationToken);
            if (!owned.IsSuccess)
            {
                return Result<SpotChangeRequestResponse>.Failure(owned.Error);
            }

            var spot = owned.Value;

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
