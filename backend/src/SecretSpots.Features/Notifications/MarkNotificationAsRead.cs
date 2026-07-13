using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Notifications;

public static class MarkNotificationAsRead
{
    public record Command(Guid NotificationId) : IRequest<Result<NotificationResponse>>;

    public class Handler(
        IAppDbContext db,
        IUserContext userContext,
        IStringLocalizer<SharedResources> localizer,
        ILogger<Handler> logger)
        : IRequestHandler<Command, Result<NotificationResponse>>
    {
        public async Task<Result<NotificationResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            var notification = await db.Notifications.SingleOrDefaultAsync(
                n => n.Id == command.NotificationId && n.UserId == userContext.UserId, cancellationToken);

            if (notification is null)
            {
                return Result<NotificationResponse>.Failure(new Error(
                    NotificationsMessageKeys.NotFound,
                    localizer[NotificationsMessageKeys.NotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            notification.IsRead = true;
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(NotificationsLogMessages.NotificationMarkedAsRead, notification.Id, userContext.UserId);

            return Result<NotificationResponse>.Success(NotificationResponseFactory.Create(notification, localizer));
        }
    }
}
