using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Auth;

public static class ExchangeExternalAuthCode
{
    public record Command(string Code) : IRequest<Result<AuthResult>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Code).NotEmpty();
        }
    }

    public class Handler(
        IAppDbContext db,
        IJwtService jwtService,
        IOptions<JwtOptions> jwtOptions,
        IStringLocalizer<SharedResources> localizer)
        : IRequestHandler<Command, Result<AuthResult>>
    {
        public async Task<Result<AuthResult>> Handle(Command command, CancellationToken cancellationToken)
        {
            var transaction = await db.ExternalAuthTransactions
                .SingleOrDefaultAsync(t => t.Id == command.Code, cancellationToken);

            var isUsable = transaction is { Status: ExternalAuthTransactionStatus.Completed, UserId: not null }
                && transaction.ExpiresAt > DateTimeOffset.UtcNow;

            if (!isUsable)
            {
                return Result<AuthResult>.Failure(new Error(
                    AuthMessageKeys.ExternalAuthCodeInvalidOrExpired,
                    localizer[AuthMessageKeys.ExternalAuthCodeInvalidOrExpired].Value,
                    StatusCodes.Status401Unauthorized));
            }

            transaction!.Status = ExternalAuthTransactionStatus.Consumed;
            await db.SaveChangesAsync(cancellationToken);

            var user = await db.Users.SingleAsync(u => u.Id == transaction.UserId!.Value, cancellationToken);

            return await AuthTokenIssuer.IssueAsync(db, jwtService, jwtOptions, user, rememberMe: true, cancellationToken);
        }
    }
}
