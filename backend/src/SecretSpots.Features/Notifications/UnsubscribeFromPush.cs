using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Notifications;

public static class UnsubscribeFromPush
{
    public record RequestBody(string Endpoint);

    public record Command(string Endpoint) : IRequest<Result<Unit>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(c => c.Endpoint).NotEmpty().WithMessage(localizer[NotificationsMessageKeys.PushEndpointRequired].Value);
        }
    }

    public class Handler(IAppDbContext db, IUserContext userContext) : IRequestHandler<Command, Result<Unit>>
    {
        public async Task<Result<Unit>> Handle(Command command, CancellationToken cancellationToken)
        {
            // Scoped to the caller's own user — deleting by endpoint alone would let anyone who
            // learns another browser's endpoint URL silently kill that person's subscription.
            await db.PushSubscriptions
                .Where(p => p.Endpoint == command.Endpoint && p.UserId == userContext.UserId)
                .ExecuteDeleteAsync(cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
