using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using NetTopologySuite.Geometries;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Validation;

namespace SecretSpots.Features.Spots;

public static class SearchNearbySpots
{
    // The map has no "load more" affordance — it shows every returned spot as a marker — so this
    // isn't a page size, it's a safety cap against a dense area returning an unbounded result set.
    // Generous for this app's scale; TotalCount below tells the frontend when a search was
    // actually truncated, so it can nudge the user to narrow the radius instead of silently
    // dropping the farthest matches.
    private const int MaxResults = 200;

    public record Query(double Latitude, double Longitude, double RadiusKm)
        : IRequest<NearbySpotsResponse>;

    public class Validator : AbstractValidator<Query>
    {
        public Validator(IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(q => q.Latitude)
                .InclusiveBetween(-90, 90).WithMessage(localizer[GeoMessageKeys.LatitudeOutOfRange].Value);

            RuleFor(q => q.Longitude)
                .InclusiveBetween(-180, 180).WithMessage(localizer[GeoMessageKeys.LongitudeOutOfRange].Value);

            RuleFor(q => q.RadiusKm)
                .ExclusiveBetween(0, 100).WithMessage(localizer[SpotsMessageKeys.RadiusOutOfRange].Value);
        }
    }

    public class Handler(IAppDbContext db) : IRequestHandler<Query, NearbySpotsResponse>
    {
        public async Task<NearbySpotsResponse> Handle(Query query, CancellationToken cancellationToken)
        {
            var searchPoint = new Point(query.Longitude, query.Latitude) { SRID = 4326 };
            var radiusMeters = query.RadiusKm * 1000;

            var withinRadius = db.Spots.Where(s => s.Location.IsWithinDistance(searchPoint, radiusMeters));

            // A plain COUNT, independent of the MaxResults cap below — lets the frontend tell the
            // user a search was truncated instead of silently dropping the farthest matches.
            var totalCount = await withinRadius.CountAsync(cancellationToken);

            // IsWithinDistance (-> ST_DWithin) filters using the GIST index first, and Distance
            // is projected once per surviving row for OrderBy. The final shaping into
            // NearbySpotResponse happens in-memory, after ToListAsync — ST_Y/ST_X (which
            // Location.Y/.X translate to) only support geometry, not geography, so extracting
            // lat/lng must happen on the materialized Point, not inside the translated query.
            var nearby = await withinRadius
                .Select(s => new { Spot = s, DistanceMeters = s.Location.Distance(searchPoint) })
                .OrderBy(x => x.DistanceMeters)
                .Take(MaxResults)
                .ToListAsync(cancellationToken);

            var items = nearby.ConvertAll(x => new NearbySpotResponse(
                x.Spot.Id,
                x.Spot.Name,
                x.Spot.Description,
                x.Spot.Category,
                x.Spot.PhotoUrls[0],
                x.Spot.Location.Y,
                x.Spot.Location.X,
                x.Spot.CreatedByUserId,
                x.Spot.CreatedAt,
                x.DistanceMeters / 1000));

            return new NearbySpotsResponse(items, totalCount);
        }
    }
}
