using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using NetTopologySuite.Geometries;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Validation;

namespace SecretSpots.Features.Trails;

public static class SearchNearbyTrails
{
    // Same rationale as SearchNearbySpots.MaxResults — a safety cap, not a page size, since the
    // map renders every returned trail as a line with no "load more" affordance.
    private const int MaxResults = 100;

    public record Query(double Latitude, double Longitude, double RadiusKm) : IRequest<NearbyTrailsResponse>;

    public class Validator : AbstractValidator<Query>
    {
        public Validator(IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(q => q.Latitude)
                .InclusiveBetween(-90, 90).WithMessage(localizer[GeoMessageKeys.LatitudeOutOfRange].Value);

            RuleFor(q => q.Longitude)
                .InclusiveBetween(-180, 180).WithMessage(localizer[GeoMessageKeys.LongitudeOutOfRange].Value);

            RuleFor(q => q.RadiusKm)
                .ExclusiveBetween(0, 100).WithMessage(localizer[TrailsMessageKeys.RadiusOutOfRange].Value);
        }
    }

    public class Handler(IAppDbContext db) : IRequestHandler<Query, NearbyTrailsResponse>
    {
        public async Task<NearbyTrailsResponse> Handle(Query query, CancellationToken cancellationToken)
        {
            var searchPoint = new Point(query.Longitude, query.Latitude) { SRID = 4326 };
            var radiusMeters = query.RadiusKm * 1000;

            var withinRadius = db.Trails.Where(t => t.Path.IsWithinDistance(searchPoint, radiusMeters));

            var totalCount = await withinRadius.CountAsync(cancellationToken);

            // Distance/coordinate extraction happens after materializing (same reasoning as
            // SearchNearbySpots) — ST_Y/ST_X-equivalent member access only translates for
            // geometry, not geography, so it can't stay inside the EF-translated query.
            var candidates = await withinRadius
                .Select(t => new { Trail = t, DistanceMeters = t.Path.Distance(searchPoint) })
                .OrderBy(x => x.DistanceMeters)
                .Take(MaxResults)
                .ToListAsync(cancellationToken);

            var items = candidates.ConvertAll(x => new NearbyTrailResponse(
                x.Trail.Id,
                x.Trail.Name,
                x.Trail.Description,
                x.Trail.PhotoUrls[0],
                x.Trail.Path.Coordinates.Select(c => new TrailPoint(c.Y, c.X)).ToList(),
                x.Trail.DistanceMeters,
                x.Trail.CreatedByUserId,
                x.Trail.CreatedAt));

            return new NearbyTrailsResponse(items, totalCount);
        }
    }
}
