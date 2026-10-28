using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Common.Storage;
using SecretSpots.Features.Spots;
using SecretSpots.Features.Notifications;

namespace SecretSpots.Features.Admin;

public static class RejectSpotChangeRequest
{
    // RequestId comes from the route, not the request body — same convention as RejectSpot.
    public record RequestBody(string Reason);

    public record Command(Guid RequestId, string Reason) : IRequest<Result<Unit>>;

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
        IPhotoStorage photoStorage,
        IStringLocalizer<SharedResources> localizer,
        ILogger<Handler> logger)
        : IRequestHandler<Command, Result<Unit>>
    {
        public async Task<Result<Unit>> Handle(Command command, CancellationToken cancellationToken)
        {
            var request = await db.SpotChangeRequests.SingleOrDefaultAsync(
                r => r.Id == command.RequestId && r.Status == SpotChangeRequestStatus.Pending, cancellationToken);
            var spot = request is null
                ? null
                : await db.Spots.SingleOrDefaultAsync(s => s.Id == request.SpotId, cancellationToken);

            if (request is null || spot is null)
            {
                return Result<Unit>.Failure(Error.NotFound(AdminMessageKeys.ChangeRequestNotFound, localizer));
            }

            request.Status = SpotChangeRequestStatus.Rejected;
            request.RejectionReason = command.Reason.Trim();
            request.ReviewedByUserId = userContext.UserId;
            request.ReviewedAt = DateTimeOffset.UtcNow;

            SpotOwnerNotifier.Add(
                db,
                request.Type == SpotChangeRequestType.Deletion ? NotificationType.SpotDeletionRejected : NotificationType.SpotEditRejected,
                spot,
                spot.Id);

            await db.SaveChangesAsync(cancellationToken);

            await SpotPhotoCleanup.DeleteUnusedAsync(
                photoStorage, request.ProposedPhotoUrls, spot.PhotoUrls, spot.Id, logger, cancellationToken);

            logger.LogInformation(AdminLogMessages.SpotChangeRequestRejected, request.Id, request.Type, spot.Id, userContext.UserId);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
