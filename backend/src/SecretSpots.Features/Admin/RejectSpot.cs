using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Spots;

namespace SecretSpots.Features.Admin;

public static class RejectSpot
{
    private const int MaxReasonLength = 500;

    // SpotId comes from the route, not the request body — same convention as SetRewardActive.
    public record RequestBody(string Reason);

    public record Command(Guid SpotId, string Reason) : IRequest<Result<SpotResponse>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(c => c.Reason)
                .NotEmpty().WithMessage(localizer[AdminMessageKeys.RejectionReasonRequired].Value)
                .MaximumLength(MaxReasonLength).WithMessage(localizer[AdminMessageKeys.RejectionReasonTooLong].Value);
        }
    }

    public class Handler(
        IAppDbContext db,
        IUserContext userContext,
        IOptions<SpotModerationOptions> spotModerationOptions,
        IStringLocalizer<SharedResources> localizer,
        ILogger<Handler> logger)
        : IRequestHandler<Command, Result<SpotResponse>>
    {
        public async Task<Result<SpotResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            var spot = await db.Spots.SingleOrDefaultAsync(s => s.Id == command.SpotId, cancellationToken);
            if (spot is null)
            {
                return Result<SpotResponse>.Failure(Error.NotFound(AdminMessageKeys.SpotNotFound, localizer));
            }

            spot.ModerationStatus = SpotModerationStatus.Rejected;
            spot.RejectionReason = command.Reason.Trim();

            // ??= so re-rejecting (e.g. to fix the reason's wording) doesn't extend the grace period.
            spot.ScheduledDeletionAt ??= DateTimeOffset.UtcNow.AddHours(spotModerationOptions.Value.RejectionGracePeriodHours);

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(AdminLogMessages.SpotRejected, spot.Id, userContext.UserId);

            var creatorDisplayName = await db.Users
                .Where(u => u.Id == spot.CreatedByUserId)
                .Select(u => u.DisplayName)
                .SingleOrDefaultAsync(cancellationToken) ?? string.Empty;

            return Result<SpotResponse>.Success(new SpotResponse(
                spot.Id,
                spot.Name,
                spot.Description,
                spot.Category,
                spot.PhotoUrls,
                spot.Location.Y,
                spot.Location.X,
                spot.CreatedByUserId,
                creatorDisplayName,
                spot.AverageRating,
                spot.RatingsCount,
                spot.CreatedAt,
                spot.ModerationStatus,
                spot.RejectionReason,
                spot.ScheduledDeletionAt));
        }
    }
}
