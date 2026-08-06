using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

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

    public class Handler(IAppDbContext db, IUserContext userContext, IStringLocalizer<SharedResources> localizer, ILogger<Handler> logger)
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
            }
            else
            {
                rating.Value = command.Value;
                rating.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await db.SaveChangesAsync(cancellationToken);

            var stats = await db.Ratings
                .Where(r => r.SpotId == spot.Id)
                .GroupBy(r => r.SpotId)
                .Select(g => new { Average = g.Average(r => r.Value), Count = g.Count() })
                .SingleAsync(cancellationToken);

            spot.AverageRating = stats.Average;
            spot.RatingsCount = stats.Count;
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(RatingsLogMessages.SpotRated, spot.Id, rating.Value, userContext.UserId);

            return Result<RatingResponse>.Success(new RatingResponse(spot.Id, rating.Value, spot.AverageRating, spot.RatingsCount));
        }
    }
}
