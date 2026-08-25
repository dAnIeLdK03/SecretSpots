using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Notifications;

public static class MarkAllNotificationsAsRead
{
    public record Command : IRequest<Result<Unit>>;

    public class Handler(IAppDbContext db, IUserContext userContext, ILogger<Handler> logger)
        : IRequestHandler<Command, Result<Unit>>
    {
        public async Task<Result<Unit>> Handle(Command command, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;

            var updatedCount = await db.Notifications
                .Where(n => n.UserId == userContext.UserId && !n.IsRead)
                .ExecuteUpdateAsync(
                    n => n.SetProperty(x => x.IsRead, true).SetProperty(x => x.ReadAt, now),
                    cancellationToken);

            if (updatedCount > 0)
            {
                logger.LogInformation(NotificationsLogMessages.AllNotificationsMarkedAsRead, updatedCount, userContext.UserId);
            }

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
