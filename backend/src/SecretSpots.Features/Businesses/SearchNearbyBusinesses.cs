using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using NetTopologySuite.Geometries;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Validation;

namespace SecretSpots.Features.Businesses;

public static class SearchNearbyBusinesses
{
    // Same rationale as SearchNearbySpots.MaxResults — this app has no "load more" for a map-style
    // nearby list, so it's a safety cap against a dense area, not a page size. TotalCount below
    // still reports the true count so the frontend can tell the user a search was truncated.
    private const int MaxResults = 200;

    public record Query(double Latitude, double Longitude, double RadiusKm) : IRequest<NearbyBusinessesResponse>;

    public class Validator : AbstractValidator<Query>
    {
        public Validator(IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(q => q.Latitude)
                .InclusiveBetween(-90, 90).WithMessage(localizer[GeoMessageKeys.LatitudeOutOfRange].Value);

            RuleFor(q => q.Longitude)
                .InclusiveBetween(-180, 180).WithMessage(localizer[GeoMessageKeys.LongitudeOutOfRange].Value);

            RuleFor(q => q.RadiusKm)
                .ExclusiveBetween(0, 100).WithMessage(localizer[BusinessesMessageKeys.RadiusOutOfRange].Value);
        }
    }

    public class Handler(IAppDbContext db) : IRequestHandler<Query, NearbyBusinessesResponse>
    {
        public async Task<NearbyBusinessesResponse> Handle(Query query, CancellationToken cancellationToken)
        {
            var searchPoint = new Point(query.Longitude, query.Latitude) { SRID = 4326 };
            var radiusMeters = query.RadiusKm * 1000;

            // Paused businesses (IsActive = false) are deliberately excluded from discovery — a
            // direct link to one still resolves via GetBusiness, same as a paused reward is still
            // visible (just not redeemable) on its own business page.
            var withinRadius = db.Businesses
                .Where(b => b.IsActive && b.Location.IsWithinDistance(searchPoint, radiusMeters));

            var totalCount = await withinRadius.CountAsync(cancellationToken);

            // Same reason as SearchNearbySpots: ST_Y/ST_X only support geometry, so lat/lng
            // extraction happens on the materialized Point after ToListAsync, not in-query.
            var nearby = await withinRadius
                .Select(b => new { Business = b, DistanceMeters = b.Location.Distance(searchPoint) })
                .OrderBy(x => x.DistanceMeters)
                .Take(MaxResults)
                .ToListAsync(cancellationToken);

            var items = nearby.ConvertAll(x => new NearbyBusinessResponse(
                x.Business.Id,
                x.Business.Name,
                x.Business.Description,
                x.Business.Location.Y,
                x.Business.Location.X,
                x.Business.IsPromoted,
                x.DistanceMeters / 1000));

            return new NearbyBusinessesResponse(items, totalCount);
        }
    }
}
