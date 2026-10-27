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
using SecretSpots.Features.Notifications;

namespace SecretSpots.Features.Admin;

public static class RejectSpot
{
    // SpotId comes from the route, not the request body — same convention as SetRewardActive.
    public record RequestBody(string Reason);

    public record Command(Guid SpotId, string Reason) : IRequest<Result<Unit>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(c => c.Reason)
                .NotEmpty().WithMessage(localizer[AdminMessageKeys.RejectionReasonRequired].Value)
                .MaximumLength(RejectionReasonRules.MaxLength)
                .WithMessage(localizer[AdminMessageKeys.RejectionReasonTooLong, RejectionReasonRules.MaxLength].Value);
        }
    }

    public class Handler(
        IAppDbContext db,
        IUserContext userContext,
        IOptions<SpotModerationOptions> spotModerationOptions,
        IStringLocalizer<SharedResources> localizer,
        ILogger<Handler> logger)
        : IRequestHandler<Command, Result<Unit>>
    {
        public async Task<Result<Unit>> Handle(Command command, CancellationToken cancellationToken)
        {
            var spot = await db.Spots.SingleOrDefaultAsync(s => s.Id == command.SpotId, cancellationToken);
            if (spot is null)
            {
                return Result<Unit>.Failure(Error.NotFound(AdminMessageKeys.SpotNotFound, localizer));
            }

            // Rejecting an Approved spot takes it down — the creator hears it as a removal.
            var notificationType = spot.ModerationStatus == SpotModerationStatus.Approved
                ? NotificationType.SpotRemoved
                : NotificationType.SpotRejected;
            var statusChanged = spot.ModerationStatus != SpotModerationStatus.Rejected;

            spot.ModerationStatus = SpotModerationStatus.Rejected;
            spot.RejectionReason = command.Reason.Trim();

            // ??= so re-rejecting (e.g. to fix the reason's wording) doesn't extend the grace period.
            spot.ScheduledDeletionAt ??= DateTimeOffset.UtcNow.AddHours(spotModerationOptions.Value.RejectionGracePeriodHours);

            if (statusChanged)
            {
                SpotOwnerNotifier.Add(db, notificationType, spot, spot.Id);
            }

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(AdminLogMessages.SpotRejected, spot.Id, userContext.UserId);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
