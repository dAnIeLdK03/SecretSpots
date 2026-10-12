using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Common.Validation;

namespace SecretSpots.Features.SavedSpots;

public static class GetMySavedSpots
{
    public record Query(int Page, int PageSize) : IRequest<PagedResponse<SavedSpotResponse>>;

    public class Validator : AbstractValidator<Query>
    {
        public Validator(IStringLocalizer<SharedResources> localizer, IOptions<SavedSpotsOptions> savedSpotsOptions)
        {
            this.ValidatePagination(
                q => q.Page,
                q => q.PageSize,
                savedSpotsOptions.Value.MaxPageSize,
                localizer[SavedSpotsMessageKeys.PageOutOfRange].Value,
                localizer[SavedSpotsMessageKeys.PageSizeOutOfRange].Value);
        }
    }

    public class Handler(IAppDbContext db, IUserContext userContext) : IRequestHandler<Query, PagedResponse<SavedSpotResponse>>
    {
        public async Task<PagedResponse<SavedSpotResponse>> Handle(Query query, CancellationToken cancellationToken)
        {
            var baseQuery =
                from savedSpot in db.SavedSpots
                join spot in db.Spots on savedSpot.SpotId equals spot.Id
                where savedSpot.UserId == userContext.UserId
                select new { savedSpot, spot };

            // Materialized first, then shaped in-memory — PhotoUrls[0] (like SearchNearbySpots)
            // can't be translated to SQL by the Npgsql provider.
            return await baseQuery
                .OrderByDescending(x => x.savedSpot.CreatedAt)
                .ToPagedResponseAsync(
                    query.Page,
                    query.PageSize,
                    x => new SavedSpotResponse(
                        x.spot.Id,
                        x.spot.Name,
                        x.spot.PhotoUrls.Count > 0 ? x.spot.PhotoUrls[0] : string.Empty,
                        x.spot.Category,
                        x.savedSpot.CreatedAt),
                    cancellationToken);
        }
    }
}
