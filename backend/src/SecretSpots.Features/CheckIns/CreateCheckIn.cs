using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Auth;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Common.Validation;

namespace SecretSpots.Features.CheckIns;

public static class CreateCheckIn
{
    // SpotId comes from the route, not the request body — kept as a separate
    // record so the endpoint can bind RequestBody from JSON and Command from both.
    public record RequestBody(string PhotoUrl, double Latitude, double Longitude);

    public record Command(
        Guid SpotId,
        string PhotoUrl,
        double Latitude,
        double Longitude) : IRequest<Result<CheckInResponse>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(c => c.PhotoUrl)
                .NotEmpty().WithMessage(localizer[CheckInsMessageKeys.PhotoUrlRequired].Value)
                .Must(UrlValidation.IsHttpUrl).WithMessage(localizer[CheckInsMessageKeys.PhotoUrlInvalid].Value);

            RuleFor(c => c.Latitude)
                .InclusiveBetween(-90, 90).WithMessage(localizer[GeoMessageKeys.LatitudeOutOfRange].Value);

            RuleFor(c => c.Longitude)
                .InclusiveBetween(-180, 180).WithMessage(localizer[GeoMessageKeys.LongitudeOutOfRange].Value);
        }
    }

    public class Handler(
        IAppDbContext db,
        IUserContext userContext,
        IOptions<CrystalsOptions> crystalsOptions,
        IOptions<CheckInOptions> checkInOptions,
        IStringLocalizer<SharedResources> localizer,
        ILogger<Handler> logger)
        : IRequestHandler<Command, Result<CheckInResponse>>
    {
        public async Task<Result<CheckInResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            var spot = await db.Spots.SingleOrDefaultAsync(s => s.Id == command.SpotId, cancellationToken);
            if (spot is null)
            {
                return Result<CheckInResponse>.Failure(new Error(
                    CheckInsMessageKeys.SpotNotFound,
                    localizer[CheckInsMessageKeys.SpotNotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            // spot is a materialized entity here, so reading .Y/.X off its Location is safe —
            // unlike SearchNearbySpots, this isn't part of a SQL-translated projection.
            var distanceMeters = HaversineDistanceCalculator.CalculateMeters(
                command.Latitude, command.Longitude, spot.Location.Y, spot.Location.X);

            if (distanceMeters > checkInOptions.Value.MaxDistanceMeters)
            {
                return Result<CheckInResponse>.Failure(new Error(
                    CheckInsMessageKeys.TooFarFromSpot,
                    localizer[CheckInsMessageKeys.TooFarFromSpot].Value,
                    StatusCodes.Status400BadRequest));
            }

            // Same "current authenticated user" edge case as GetCurrentUser — handled the
            // same way (graceful 404), not an unhandled exception, in case the JWT outlives
            // the user row (e.g. account deleted after the token was issued).
            var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);
            if (user is null)
            {
                return Result<CheckInResponse>.Failure(new Error(
                    AuthMessageKeys.UserNotFound,
                    localizer[AuthMessageKeys.UserNotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            var reward = crystalsOptions.Value.CheckInReward;
            user.CrystalBalance += reward;

            var checkIn = new CheckIn
            {
                Id = Guid.NewGuid(),
                SpotId = spot.Id,
                UserId = userContext.UserId,
                PhotoUrl = command.PhotoUrl,
                CrystalsAwarded = reward,
            };

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userContext.UserId,
                Type = NotificationType.CrystalsEarned,
                RelatedSpotId = spot.Id,
                CrystalsAwarded = reward,
            };

            db.CheckIns.Add(checkIn);
            db.Notifications.Add(notification);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                CheckInsLogMessages.CheckInCreated, checkIn.Id, spot.Id, user.Id, reward);

            return Result<CheckInResponse>.Success(new CheckInResponse(
                checkIn.Id,
                spot.Id,
                checkIn.PhotoUrl,
                reward,
                user.CrystalBalance,
                checkIn.CreatedAt));
        }
    }
}
