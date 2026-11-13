using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.ExceptionHandling;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Common.Validation;
using SecretSpots.Features.Notifications;

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
        public Validator(IStringLocalizer<SharedResources> localizer, IOptions<R2Options> r2Options, IUserContext userContext)
        {
            RuleFor(c => c.PhotoUrl)
                .NotEmpty().WithMessage(localizer[CheckInsMessageKeys.PhotoUrlRequired].Value)
                .Must(url => UrlValidation.IsOwnPhotoUrl(url, r2Options.Value.PublicBaseUrl, userContext.UserId))
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
        IOptions<CheckInOptions> checkInOptions,
        TimeProvider timeProvider,
        IStringLocalizer<SharedResources> localizer,
        ILogger<Handler> logger)
        : IRequestHandler<Command, Result<CheckInResponse>>
    {
        public async Task<Result<CheckInResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            if (userContext.Role == UserRole.Admin)
            {
                return Result<CheckInResponse>.Failure(Error.Forbidden(CheckInsMessageKeys.AdminsCannotCheckIn, localizer));
            }

            var spot = await db.Spots.SingleOrDefaultAsync(s => s.Id == command.SpotId, cancellationToken);
            if (spot is null)
            {
                return Result<CheckInResponse>.Failure(Error.NotFound(CheckInsMessageKeys.SpotNotFound, localizer));
            }

            // A check-in is proof that someone else found the place — the creator set the
            // coordinates themselves, so checking in at their own spot would prove nothing and
            // only inflate its visits.
            if (spot.CreatedByUserId == userContext.UserId)
            {
                return Result<CheckInResponse>.Failure(Error.BadRequest(CheckInsMessageKeys.CannotCheckInOwnSpot, localizer));
            }

            // spot is a materialized entity here, so reading .Y/.X off its Location is safe —
            // unlike SearchNearbySpots, this isn't part of a SQL-translated projection.
            var distanceMeters = HaversineDistanceCalculator.CalculateMeters(
                command.Latitude, command.Longitude, spot.Location.Y, spot.Location.X);

            if (distanceMeters > checkInOptions.Value.MaxDistanceMeters)
            {
                return Result<CheckInResponse>.Failure(Error.BadRequest(CheckInsMessageKeys.TooFarFromSpot, localizer));
            }

            // One check-in per spot per user within the cooldown window, so a spot's visits
            // can't be inflated by checking in over and over.
            var cooldownStart = timeProvider.GetUtcNow().AddHours(-checkInOptions.Value.CooldownHours);
            var checkedInRecently = await db.CheckIns.AnyAsync(
                c => c.SpotId == command.SpotId && c.UserId == userContext.UserId && c.CreatedAt > cooldownStart,
                cancellationToken);

            if (checkedInRecently)
            {
                return Result<CheckInResponse>.Failure(Error.BadRequest(CheckInsMessageKeys.TooSoonSinceLastCheckIn, localizer));
            }

            // Same "current authenticated user" edge case as GetCurrentUser — handled the
            // same way (graceful 404), not an unhandled exception, in case the JWT outlives
            // the user row (e.g. account deleted after the token was issued).
            if (!await db.Users.AnyAsync(u => u.Id == userContext.UserId, cancellationToken))
            {
                return Result<CheckInResponse>.Failure(Error.NotFound(CommonMessageKeys.CurrentUserNotFound, localizer));
            }

            // The spot's creator hears about it the first time each visitor checks in — only the
            // first, so repeat visits by the same person don't keep notifying them.
            var isFirstVisitByUser = !await db.CheckIns.AnyAsync(
                c => c.SpotId == spot.Id && c.UserId == userContext.UserId, cancellationToken);
            if (isFirstVisitByUser)
            {
                db.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = spot.CreatedByUserId,
                    Type = NotificationType.NewVisitorOnYourSpot,
                    RelatedSpotId = spot.Id,
                });
            }

            var checkIn = new CheckIn
            {
                Id = Guid.NewGuid(),
                SpotId = spot.Id,
                UserId = userContext.UserId,
                PhotoUrl = command.PhotoUrl,
            };

            db.CheckIns.Add(checkIn);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                CheckInsLogMessages.CheckInCreated, checkIn.Id, spot.Id, userContext.UserId);

            return Result<CheckInResponse>.Success(new CheckInResponse(
                checkIn.Id,
                spot.Id,
                checkIn.PhotoUrl,
                checkIn.CreatedAt));
        }
    }
}
