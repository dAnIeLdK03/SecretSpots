using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Auth;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.ExceptionHandling;
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
        public Validator(IStringLocalizer<SharedResources> localizer, IOptions<R2Options> r2Options)
        {
            RuleFor(c => c.PhotoUrl)
                .NotEmpty().WithMessage(localizer[CheckInsMessageKeys.PhotoUrlRequired].Value)
                .Must(url => UrlValidation.IsOwnPhotoUrl(url, r2Options.Value.PublicBaseUrl))
                .WithMessage(localizer[CheckInsMessageKeys.PhotoUrlInvalid].Value);

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

            // Otherwise a user could farm unlimited crystals for free: create a spot at their
            // current location (trivially satisfies the distance check below since they set the
            // coordinates themselves), check in once, repeat with a new spot. The per-spot
            // cooldown below only stops repeat check-ins on the *same* spot, not this.
            if (spot.CreatedByUserId == userContext.UserId)
            {
                return Result<CheckInResponse>.Failure(new Error(
                    CheckInsMessageKeys.CannotCheckInOwnSpot,
                    localizer[CheckInsMessageKeys.CannotCheckInOwnSpot].Value,
                    StatusCodes.Status400BadRequest));
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

            // Stops trivially farming crystals by repeatedly checking in at the same spot —
            // one crystal-earning check-in per spot per user within the cooldown window.
            var cooldownStart = DateTimeOffset.UtcNow.AddHours(-checkInOptions.Value.CooldownHours);
            var checkedInRecently = await db.CheckIns.AnyAsync(
                c => c.SpotId == command.SpotId && c.UserId == userContext.UserId && c.CreatedAt > cooldownStart,
                cancellationToken);

            if (checkedInRecently)
            {
                return Result<CheckInResponse>.Failure(new Error(
                    CheckInsMessageKeys.TooSoonSinceLastCheckIn,
                    localizer[CheckInsMessageKeys.TooSoonSinceLastCheckIn].Value,
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

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                // This user's balance changed concurrently (another check-in, a redemption) —
                // reject rather than silently overwrite it with a stale value.
                return Result<CheckInResponse>.Failure(new Error(
                    CommonMessageKeys.ConcurrencyConflict,
                    localizer[CommonMessageKeys.ConcurrencyConflict].Value,
                    StatusCodes.Status409Conflict));
            }

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
