using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Common.Validation;

namespace SecretSpots.Features.Notifications;

public static class GetNotifications
{
    public record Query(int Page, int PageSize) : IRequest<PagedResponse<NotificationResponse>>;

    public class Validator : AbstractValidator<Query>
    {
        public Validator(IStringLocalizer<SharedResources> localizer, IOptions<NotificationsOptions> notificationsOptions)
        {
            this.ValidatePagination(
                q => q.Page,
                q => q.PageSize,
                notificationsOptions.Value.MaxPageSize,
                localizer[NotificationsMessageKeys.PageOutOfRange].Value,
                localizer[NotificationsMessageKeys.PageSizeOutOfRange].Value);
        }
    }

    public class Handler(
        IAppDbContext db,
        IUserContext userContext,
        IOptions<NotificationsOptions> notificationsOptions,
        IStringLocalizer<SharedResources> localizer)
        : IRequestHandler<Query, PagedResponse<NotificationResponse>>
    {
        public async Task<PagedResponse<NotificationResponse>> Handle(Query query, CancellationToken cancellationToken)
        {
            // Read notifications age out of the default listing after ReadRetentionHours —
            // unread ones are always included regardless of how old they are. The row is never
            // deleted, so this only affects what shows up here.
            var readCutoff = DateTimeOffset.UtcNow.AddHours(-notificationsOptions.Value.ReadRetentionHours);

            var baseQuery = db.Notifications
                .Where(n => n.UserId == userContext.UserId && (!n.IsRead || n.ReadAt >= readCutoff));

            return await baseQuery
                .OrderByDescending(n => n.CreatedAt)
                .ToPagedResponseAsync(
                    query.Page,
                    query.PageSize,
                    n => NotificationResponseFactory.Create(n, localizer),
                    cancellationToken);
        }
    }
}
