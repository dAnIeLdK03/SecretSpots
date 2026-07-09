using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Auth;

public static class RefreshAccessToken
{
    public record Command(string RefreshToken) : IRequest<Result<AuthResult>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(c => c.RefreshToken)
                .NotEmpty().WithMessage(localizer[AuthMessageKeys.RefreshTokenRequired].Value);
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
            var existingToken = await db.RefreshTokens
                .SingleOrDefaultAsync(t => t.Token == command.RefreshToken, cancellationToken);

            var isUsable = existingToken is { RevokedAt: null } && existingToken.ExpiresAt > DateTimeOffset.UtcNow;

            if (!isUsable)
            {
                return Result<AuthResult>.Failure(new Error(
                    AuthMessageKeys.InvalidOrExpiredRefreshToken,
                    localizer[AuthMessageKeys.InvalidOrExpiredRefreshToken].Value,
                    StatusCodes.Status401Unauthorized));
            }

            var user = await db.Users
                .SingleAsync(u => u.Id == existingToken!.UserId, cancellationToken);

            existingToken!.RevokedAt = DateTimeOffset.UtcNow;

            return await AuthTokenIssuer.IssueAsync(db, jwtService, jwtOptions, user, cancellationToken);
        }
    }
}
