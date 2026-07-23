using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Common.Validation;

namespace SecretSpots.Features.Spots;

public static class CreateSpot
{
    public record Command(
        string Name,
        string Description,
        SpotCategory Category,
        string PhotoUrl,
        double Latitude,
        double Longitude) : IRequest<SpotResponse>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(c => c.Name)
                .NotEmpty().WithMessage(localizer[SpotsMessageKeys.NameRequired].Value)
                .MaximumLength(100).WithMessage(localizer[SpotsMessageKeys.NameTooLong].Value);

            RuleFor(c => c.Description)
                .NotEmpty().WithMessage(localizer[SpotsMessageKeys.DescriptionRequired].Value)
                .MaximumLength(2000).WithMessage(localizer[SpotsMessageKeys.DescriptionTooLong].Value);

            RuleFor(c => c.PhotoUrl)
                .NotEmpty().WithMessage(localizer[SpotsMessageKeys.PhotoUrlRequired].Value)
                .Must(UrlValidation.IsHttpUrl).WithMessage(localizer[SpotsMessageKeys.PhotoUrlInvalid].Value);

            RuleFor(c => c.Category)
                .IsInEnum().WithMessage(localizer[SpotsMessageKeys.InvalidCategory].Value);

            RuleFor(c => c.Latitude)
                .InclusiveBetween(-90, 90).WithMessage(localizer[GeoMessageKeys.LatitudeOutOfRange].Value);

            RuleFor(c => c.Longitude)
                .InclusiveBetween(-180, 180).WithMessage(localizer[GeoMessageKeys.LongitudeOutOfRange].Value);
        }
    }

    public class Handler(
        IAppDbContext db, IUserContext userContext, IOptions<NotificationsOptions> notificationsOptions, ILogger<Handler> logger)
        : IRequestHandler<Command, SpotResponse>
    {
        public async Task<SpotResponse> Handle(Command command, CancellationToken cancellationToken)
        {
            var location = new Point(command.Longitude, command.Latitude) { SRID = 4326 };

            var spot = new Spot
            {
                Id = Guid.NewGuid(),
                Name = command.Name.Trim(),
                Description = command.Description.Trim(),
                Category = command.Category,
                PhotoUrl = command.PhotoUrl,
                Location = location,
                CreatedByUserId = userContext.UserId,
            };

            // Queried before the new spot is added to the context, so it can't match itself —
            // there's no persisted "user location" (see #NN discussion), so "nearby" is a proxy:
            // anyone who already created a spot or checked in within range of this one.
            var radiusMeters = notificationsOptions.Value.NewSpotRadiusKm * 1000;

            var creatorsNearby = db.Spots
                .Where(s => s.Location.IsWithinDistance(location, radiusMeters) && s.CreatedByUserId != userContext.UserId)
                .Select(s => s.CreatedByUserId);

            var checkedInUsersNearby =
                from checkIn in db.CheckIns
                join checkedInSpot in db.Spots on checkIn.SpotId equals checkedInSpot.Id
                where checkedInSpot.Location.IsWithinDistance(location, radiusMeters) && checkIn.UserId != userContext.UserId
                select checkIn.UserId;

            var nearbyUserIds = await creatorsNearby.Union(checkedInUsersNearby).ToListAsync(cancellationToken);

            db.Spots.Add(spot);

            foreach (var userId in nearbyUserIds)
            {
                db.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Type = NotificationType.NewSpotNearby,
                    RelatedSpotId = spot.Id,
                });
            }

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(SpotsLogMessages.SpotCreated, spot.Id, spot.Category, spot.CreatedByUserId);
            logger.LogInformation(SpotsLogMessages.NearbyUsersNotified, nearbyUserIds.Count, spot.Id);

            return new SpotResponse(
                spot.Id,
                spot.Name,
                spot.Description,
                spot.Category,
                spot.PhotoUrl,
                location.Y,
                location.X,
                spot.CreatedByUserId,
                spot.CreatedAt);
        }
    }
}
