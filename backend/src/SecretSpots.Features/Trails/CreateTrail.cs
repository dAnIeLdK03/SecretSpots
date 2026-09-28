using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using SecretSpots.Domain;
using SecretSpots.Features.CheckIns;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Common.Validation;

namespace SecretSpots.Features.Trails;

public static class CreateTrail
{
    public const int MaxPhotoCount = 5;
    public const int MinPointCount = 2;
    public const int MaxPointCount = 500;

    public record Command(
        string Name,
        string Description,
        IReadOnlyList<string> PhotoUrls,
        IReadOnlyList<TrailPoint> Points) : IRequest<TrailResponse>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IStringLocalizer<SharedResources> localizer, IOptions<R2Options> r2Options, IUserContext userContext)
        {
            RuleFor(c => c.Name)
                .NotEmpty().WithMessage(localizer[TrailsMessageKeys.NameRequired].Value)
                .MaximumLength(100).WithMessage(localizer[TrailsMessageKeys.NameTooLong].Value);

            RuleFor(c => c.Description)
                .NotEmpty().WithMessage(localizer[TrailsMessageKeys.DescriptionRequired].Value)
                .MaximumLength(2000).WithMessage(localizer[TrailsMessageKeys.DescriptionTooLong].Value);

            RuleFor(c => c.PhotoUrls)
                .NotEmpty().WithMessage(localizer[TrailsMessageKeys.PhotoUrlRequired].Value)
                .Must(urls => urls.Count <= MaxPhotoCount).WithMessage(localizer[TrailsMessageKeys.PhotoUrlsTooMany].Value);

            RuleForEach(c => c.PhotoUrls)
                .Must(url => UrlValidation.IsOwnPhotoUrl(url, r2Options.Value.PublicBaseUrl, userContext.UserId))
                .WithMessage(localizer[TrailsMessageKeys.PhotoUrlInvalid].Value);

            RuleFor(c => c.Points)
                .Must(points => points.Count >= MinPointCount).WithMessage(localizer[TrailsMessageKeys.PointsRequired].Value)
                .Must(points => points.Count <= MaxPointCount).WithMessage(localizer[TrailsMessageKeys.PointsTooMany].Value);

            RuleForEach(c => c.Points).ChildRules(point =>
            {
                point.RuleFor(p => p.Latitude)
                    .InclusiveBetween(-90, 90).WithMessage(localizer[GeoMessageKeys.LatitudeOutOfRange].Value);

                point.RuleFor(p => p.Longitude)
                    .InclusiveBetween(-180, 180).WithMessage(localizer[GeoMessageKeys.LongitudeOutOfRange].Value);
            });
        }
    }

    public class Handler(IAppDbContext db, IUserContext userContext, ILogger<Handler> logger)
        : IRequestHandler<Command, TrailResponse>
    {
        public async Task<TrailResponse> Handle(Command command, CancellationToken cancellationToken)
        {
            var coordinates = command.Points
                .Select(p => new Coordinate(p.Longitude, p.Latitude))
                .ToArray();
            var path = new LineString(coordinates) { SRID = 4326 };

            var distanceMeters = 0.0;
            for (var i = 1; i < command.Points.Count; i++)
            {
                var prev = command.Points[i - 1];
                var next = command.Points[i];
                distanceMeters += HaversineDistanceCalculator.CalculateMeters(prev.Latitude, prev.Longitude, next.Latitude, next.Longitude);
            }

            var trail = new Trail
            {
                Id = Guid.NewGuid(),
                Name = command.Name.Trim(),
                Description = command.Description.Trim(),
                Path = path,
                DistanceMeters = distanceMeters,
                PhotoUrls = command.PhotoUrls.ToList(),
                CreatedByUserId = userContext.UserId,
            };

            db.Trails.Add(trail);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(TrailsLogMessages.TrailCreated, trail.Id, command.Points.Count, distanceMeters, userContext.UserId);

            var creatorDisplayName = await db.Users
                .Where(u => u.Id == trail.CreatedByUserId)
                .Select(u => u.DisplayName)
                .SingleOrDefaultAsync(cancellationToken) ?? string.Empty;

            return new TrailResponse(
                trail.Id,
                trail.Name,
                trail.Description,
                trail.PhotoUrls,
                command.Points,
                trail.DistanceMeters,
                trail.CreatedByUserId,
                creatorDisplayName,
                trail.CreatedAt);
        }
    }
}
