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
    private const int MaxResults = 50;

    public record Query(double Latitude, double Longitude, double RadiusKm)
        : IRequest<IReadOnlyList<NearbySpotResponse>>;

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

    public class Handler(IAppDbContext db) : IRequestHandler<Query, IReadOnlyList<NearbySpotResponse>>
    {
        public async Task<IReadOnlyList<NearbySpotResponse>> Handle(Query query, CancellationToken cancellationToken)
        {
            var searchPoint = new Point(query.Longitude, query.Latitude) { SRID = 4326 };
            var radiusMeters = query.RadiusKm * 1000;

            // IsWithinDistance (-> ST_DWithin) filters using the GIST index first, and Distance
            // is projected once per surviving row for OrderBy. The final shaping into
            // NearbySpotResponse happens in-memory, after ToListAsync — ST_Y/ST_X (which
            // Location.Y/.X translate to) only support geometry, not geography, so extracting
            // lat/lng must happen on the materialized Point, not inside the translated query.
            var nearby = await db.Spots
                .Where(s => s.Location.IsWithinDistance(searchPoint, radiusMeters))
                .Select(s => new { Spot = s, DistanceMeters = s.Location.Distance(searchPoint) })
                .OrderBy(x => x.DistanceMeters)
                .Take(MaxResults)
                .ToListAsync(cancellationToken);

            return nearby.ConvertAll(x => new NearbySpotResponse(
                x.Spot.Id,
                x.Spot.Name,
                x.Spot.Description,
                x.Spot.Category,
                x.Spot.PhotoUrl,
                x.Spot.Location.Y,
                x.Spot.Location.X,
                x.Spot.CreatedByUserId,
                x.Spot.CreatedAt,
                x.DistanceMeters / 1000));
        }
    }
}
