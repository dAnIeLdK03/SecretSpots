using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Notifications;

namespace SecretSpots.Features.Admin;

public static class ApproveSpot
{
    public record Command(Guid SpotId) : IRequest<Result<Unit>>;

    public class Handler(IAppDbContext db, IUserContext userContext, IStringLocalizer<SharedResources> localizer, ILogger<Handler> logger)
        : IRequestHandler<Command, Result<Unit>>
    {
        public async Task<Result<Unit>> Handle(Command command, CancellationToken cancellationToken)
        {
            var spot = await db.Spots.SingleOrDefaultAsync(s => s.Id == command.SpotId, cancellationToken);
            if (spot is null)
            {
                return Result<Unit>.Failure(Error.NotFound(AdminMessageKeys.SpotNotFound, localizer));
            }

            // The rules (approve vs. restore, what gets cleared) live in Spot.Approve.
            if (spot.Approve() is { } notificationType)
            {
                SpotOwnerNotifier.Add(db, notificationType, spot, spot.Id);
            }

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(AdminLogMessages.SpotApproved, spot.Id, userContext.UserId);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
