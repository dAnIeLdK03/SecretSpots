using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;

namespace SecretSpots.Features.Spots;

public static class GetSpot
{
    public record Query(Guid SpotId) : IRequest<Result<SpotResponse>>;

    public class Handler(IAppDbContext db, IStringLocalizer<SharedResources> localizer)
        : IRequestHandler<Query, Result<SpotResponse>>
    {
        public async Task<Result<SpotResponse>> Handle(Query query, CancellationToken cancellationToken)
        {
            var spot = await db.Spots.SingleOrDefaultAsync(s => s.Id == query.SpotId, cancellationToken);
            if (spot is null)
            {
                return Result<SpotResponse>.Failure(new Error(
                    SpotsMessageKeys.NotFound,
                    localizer[SpotsMessageKeys.NotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            return Result<SpotResponse>.Success(new SpotResponse(
                spot.Id,
                spot.Name,
                spot.Description,
                spot.Category,
                spot.PhotoUrl,
                spot.Location.Y,
                spot.Location.X,
                spot.CreatedByUserId,
                spot.CreatedAt));
        }
    }
}
