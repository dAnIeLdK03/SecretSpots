using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.SavedSpots;

public static class GetMySavedSpots
{
    public record Query(int Page, int PageSize) : IRequest<SavedSpotsPageResponse>;

    public class Validator : AbstractValidator<Query>
    {
        public Validator(IStringLocalizer<SharedResources> localizer, IOptions<SavedSpotsOptions> savedSpotsOptions)
        {
            RuleFor(q => q.Page)
                .GreaterThanOrEqualTo(1).WithMessage(localizer[SavedSpotsMessageKeys.PageOutOfRange].Value);

            RuleFor(q => q.PageSize)
                .InclusiveBetween(1, savedSpotsOptions.Value.MaxPageSize)
                    .WithMessage(localizer[SavedSpotsMessageKeys.PageSizeOutOfRange].Value);
        }
    }

    public class Handler(IAppDbContext db, IUserContext userContext) : IRequestHandler<Query, SavedSpotsPageResponse>
    {
        public async Task<SavedSpotsPageResponse> Handle(Query query, CancellationToken cancellationToken)
        {
            var baseQuery =
                from savedSpot in db.SavedSpots
                join spot in db.Spots on savedSpot.SpotId equals spot.Id
                where savedSpot.UserId == userContext.UserId
                select new { savedSpot, spot };

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            // Materialized first, then shaped in-memory — PhotoUrls[0] (like SearchNearbySpots)
            // can't be translated to SQL by the Npgsql provider.
            var page = await baseQuery
                .OrderByDescending(x => x.savedSpot.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            var items = page.ConvertAll(x => new SavedSpotResponse(
                x.spot.Id,
                x.spot.Name,
                x.spot.PhotoUrls.Count > 0 ? x.spot.PhotoUrls[0] : string.Empty,
                x.spot.Category,
                x.savedSpot.CreatedAt));

            return new SavedSpotsPageResponse(items, query.Page, query.PageSize, totalCount);
        }
    }
}
