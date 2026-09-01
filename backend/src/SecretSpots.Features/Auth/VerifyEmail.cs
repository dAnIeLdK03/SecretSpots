using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Auth;

public static class VerifyEmail
{
    public record Command(string Token) : IRequest<Result<Unit>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Token).NotEmpty();
        }
    }

    public class Handler(IAppDbContext db, IStringLocalizer<SharedResources> localizer, ILogger<Handler> logger)
        : IRequestHandler<Command, Result<Unit>>
    {
        public async Task<Result<Unit>> Handle(Command command, CancellationToken cancellationToken)
        {
            var token = await db.EmailVerificationTokens
                .SingleOrDefaultAsync(t => t.Token == OpaqueTokenHasher.Hash(command.Token), cancellationToken);

            var isUsable = token is { UsedAt: null } && token.ExpiresAt > DateTimeOffset.UtcNow;
            if (!isUsable)
            {
                return Result<Unit>.Failure(new Error(
                    AuthMessageKeys.EmailVerificationTokenInvalidOrExpired,
                    localizer[AuthMessageKeys.EmailVerificationTokenInvalidOrExpired].Value,
                    StatusCodes.Status400BadRequest));
            }

            var user = await db.Users.SingleAsync(u => u.Id == token!.UserId, cancellationToken);
            user.IsEmailVerified = true;
            token!.UsedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(AuthLogMessages.EmailVerificationCompleted, user.Id);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
