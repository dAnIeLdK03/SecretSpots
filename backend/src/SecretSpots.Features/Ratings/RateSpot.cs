using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Notifications;
using WebPush;

namespace SecretSpots.Features.Ratings;

public static class RateSpot
{
    // SpotId comes from the route, not the request body — kept as a separate
    // record so the endpoint can bind RequestBody from JSON and Command from both.
    public record RequestBody(int Value);

    public record Command(Guid SpotId, int Value) : IRequest<Result<RatingResponse>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(c => c.Value)
                .InclusiveBetween(1, 5).WithMessage(localizer[RatingsMessageKeys.ValueOutOfRange].Value);
        }
    }

    public class Handler(
        IAppDbContext db,
        IUserContext userContext,
        WebPushClient webPushClient,
        IOptions<WebPushOptions> webPushOptions,
        IStringLocalizer<SharedResources> localizer,
        ILogger<Handler> logger)
        : IRequestHandler<Command, Result<RatingResponse>>
    {
        public async Task<Result<RatingResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            var spot = await db.Spots.SingleOrDefaultAsync(s => s.Id == command.SpotId, cancellationToken);
            if (spot is null)
            {
                return Result<RatingResponse>.Failure(new Error(
                    RatingsMessageKeys.SpotNotFound,
                    localizer[RatingsMessageKeys.SpotNotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            var rating = await db.Ratings.SingleOrDefaultAsync(
                r => r.SpotId == command.SpotId && r.UserId == userContext.UserId, cancellationToken);

            Notification? notification = null;

            if (rating is null)
            {
                rating = new Rating
                {
                    Id = Guid.NewGuid(),
                    SpotId = spot.Id,
                    UserId = userContext.UserId,
                    Value = command.Value,
                };
                db.Ratings.Add(rating);

                // Only on the first rating — this is an upsert (re-rating changes Value on the
                // same row), and re-notifying on every change would just be spam.
                if (spot.CreatedByUserId != userContext.UserId)
                {
                    notification = new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = spot.CreatedByUserId,
                        Type = NotificationType.NewRatingOnYourSpot,
                        RelatedSpotId = spot.Id,
                    };
                    db.Notifications.Add(notification);
                }
            }
            else
            {
                rating.Value = command.Value;
                rating.UpdatedAt = DateTimeOffset.UtcNow;
            }

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Another concurrent request (double-click, two tabs) inserted this user's first
                // rating for this spot between the check above and this write — the unique
                // (SpotId, UserId) index caught it. Remove()-ing an entity that's still in the
                // Added state (never made it to the database) is EF Core's way to detach it
                // without needing direct ChangeTracker access, which IAppDbContext doesn't
                // expose. Fall back to updating the row the other request created, so this
                // request's Value still takes effect, instead of just discarding it.
                db.Ratings.Remove(rating);
                if (notification is not null)
                {
                    db.Notifications.Remove(notification);
                    notification = null;
                }

                rating = await db.Ratings.SingleAsync(
                    r => r.SpotId == command.SpotId && r.UserId == userContext.UserId, cancellationToken);
                rating.Value = command.Value;
                rating.UpdatedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }

            var stats = await db.Ratings
                .Where(r => r.SpotId == spot.Id)
                .GroupBy(r => r.SpotId)
                .Select(g => new { Average = g.Average(r => r.Value), Count = g.Count() })
                .SingleAsync(cancellationToken);

            spot.AverageRating = stats.Average;
            spot.RatingsCount = stats.Count;
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(RatingsLogMessages.SpotRated, spot.Id, rating.Value, userContext.UserId);

            if (notification is not null)
            {
                await PushNotificationSender.SendAsync(
                    db, webPushClient, webPushOptions, localizer, logger, notification, cancellationToken);
            }

            return Result<RatingResponse>.Success(new RatingResponse(spot.Id, rating.Value, spot.AverageRating, spot.RatingsCount));
        }
    }
}
