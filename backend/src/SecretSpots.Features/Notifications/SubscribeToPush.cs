using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Notifications;

public static class SubscribeToPush
{
    public record RequestBody(string Endpoint, string P256dh, string Auth);

    public record Command(string Endpoint, string P256dh, string Auth) : IRequest<Result<Unit>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(c => c.Endpoint).NotEmpty().WithMessage(localizer[NotificationsMessageKeys.PushEndpointRequired].Value);
            RuleFor(c => c.P256dh).NotEmpty().WithMessage(localizer[NotificationsMessageKeys.PushKeysRequired].Value);
            RuleFor(c => c.Auth).NotEmpty().WithMessage(localizer[NotificationsMessageKeys.PushKeysRequired].Value);
        }
    }

    public class Handler(IAppDbContext db, IUserContext userContext) : IRequestHandler<Command, Result<Unit>>
    {
        public async Task<Result<Unit>> Handle(Command command, CancellationToken cancellationToken)
        {
            // Same browser subscription re-registering (e.g. a stale row from before a previous
            // unsubscribe, or the same endpoint re-subscribing after clearing site data) — upsert
            // rather than accumulate duplicate rows that would each get pushed to independently.
            var existing = await db.PushSubscriptions
                .SingleOrDefaultAsync(p => p.Endpoint == command.Endpoint, cancellationToken);

            if (existing is not null)
            {
                existing.UserId = userContext.UserId;
                existing.P256dh = command.P256dh;
                existing.Auth = command.Auth;
            }
            else
            {
                db.PushSubscriptions.Add(new PushSubscription
                {
                    Id = Guid.NewGuid(),
                    UserId = userContext.UserId,
                    Endpoint = command.Endpoint,
                    P256dh = command.P256dh,
                    Auth = command.Auth,
                });
            }

            await db.SaveChangesAsync(cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
