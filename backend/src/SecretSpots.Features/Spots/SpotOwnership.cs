using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Spots;

// The "this spot exists and it's yours" check every creator-only spot action starts with
// (submitting/cancelling change requests, reading your own request).
public static class SpotOwnership
{
    public static async Task<Result<Spot>> LoadOwnedAsync(
        IAppDbContext db,
        Guid spotId,
        IUserContext userContext,
        IStringLocalizer<SharedResources> localizer,
        CancellationToken cancellationToken)
    {
        var spot = await db.Spots.SingleOrDefaultAsync(s => s.Id == spotId, cancellationToken);
        if (spot is null)
        {
            return Result<Spot>.Failure(Error.NotFound(SpotsMessageKeys.NotFound, localizer));
        }

        if (spot.CreatedByUserId != userContext.UserId)
        {
            return Result<Spot>.Failure(Error.Forbidden(SpotsMessageKeys.NotYourSpot, localizer));
        }

        return Result<Spot>.Success(spot);
    }
}
