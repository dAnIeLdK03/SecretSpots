using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Notifications;

public static class GetNotifications
{
    public record Query(int Page, int PageSize) : IRequest<NotificationsPageResponse>;

    public class Validator : AbstractValidator<Query>
    {
        public Validator(IStringLocalizer<SharedResources> localizer, IOptions<NotificationsOptions> notificationsOptions)
        {
            RuleFor(q => q.Page)
                .GreaterThanOrEqualTo(1).WithMessage(localizer[NotificationsMessageKeys.PageOutOfRange].Value);

            RuleFor(q => q.PageSize)
                .InclusiveBetween(1, notificationsOptions.Value.MaxPageSize)
                    .WithMessage(localizer[NotificationsMessageKeys.PageSizeOutOfRange].Value);
        }
    }

    public class Handler(
        IAppDbContext db,
        IUserContext userContext,
        IOptions<NotificationsOptions> notificationsOptions,
        IStringLocalizer<SharedResources> localizer)
        : IRequestHandler<Query, NotificationsPageResponse>
    {
        public async Task<NotificationsPageResponse> Handle(Query query, CancellationToken cancellationToken)
        {
            // Read notifications age out of the default listing after ReadRetentionHours —
            // unread ones are always included regardless of how old they are. The row is never
            // deleted, so this only affects what shows up here.
            var readCutoff = DateTimeOffset.UtcNow.AddHours(-notificationsOptions.Value.ReadRetentionHours);

            var baseQuery = db.Notifications
                .Where(n => n.UserId == userContext.UserId && (!n.IsRead || n.ReadAt >= readCutoff));

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var notifications = await baseQuery
                .OrderByDescending(n => n.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            var items = notifications
                .Select(n => NotificationResponseFactory.Create(n, localizer))
                .ToList();

            return new NotificationsPageResponse(items, query.Page, query.PageSize, totalCount);
        }
    }
}
