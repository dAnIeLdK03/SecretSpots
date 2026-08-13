using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;

namespace SecretSpots.Features.Auth;

public static class ResetPassword
{
    public record Command(string Token, string NewPassword) : IRequest<Result<Unit>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(c => c.Token).NotEmpty();

            RuleFor(c => c.NewPassword)
                .NotEmpty().WithMessage(localizer[AuthMessageKeys.PasswordRequired].Value)
                .MinimumLength(8).WithMessage(localizer[AuthMessageKeys.PasswordTooShort].Value)
                .Must(PasswordRules.ContainUpperCase).WithMessage(localizer[AuthMessageKeys.PasswordRequiresUpper].Value)
                .Must(PasswordRules.ContainLowerCase).WithMessage(localizer[AuthMessageKeys.PasswordRequiresLower].Value)
                .Must(PasswordRules.ContainDigit).WithMessage(localizer[AuthMessageKeys.PasswordRequiresDigit].Value)
                .Must(PasswordRules.NotBeCommonPassword).WithMessage(localizer[AuthMessageKeys.PasswordIsCommon].Value);
        }
    }

    public class Handler(IAppDbContext db, IStringLocalizer<SharedResources> localizer, ILogger<Handler> logger)
        : IRequestHandler<Command, Result<Unit>>
    {
        public async Task<Result<Unit>> Handle(Command command, CancellationToken cancellationToken)
        {
            var token = await db.PasswordResetTokens
                .SingleOrDefaultAsync(t => t.Token == command.Token, cancellationToken);

            var isUsable = token is { UsedAt: null } && token.ExpiresAt > DateTimeOffset.UtcNow;
            if (!isUsable)
            {
                return Result<Unit>.Failure(new Error(
                    AuthMessageKeys.PasswordResetTokenInvalidOrExpired,
                    localizer[AuthMessageKeys.PasswordResetTokenInvalidOrExpired].Value,
                    StatusCodes.Status400BadRequest));
            }

            var user = await db.Users.SingleAsync(u => u.Id == token!.UserId, cancellationToken);
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(command.NewPassword, workFactor: 12);
            token!.UsedAt = DateTimeOffset.UtcNow;

            // Reset invalidates every existing session — if someone else's password reset
            // request is what's happening here, this also kicks out whoever holds the
            // account's current refresh tokens.
            var activeRefreshTokens = await db.RefreshTokens
                .Where(t => t.UserId == user.Id && t.RevokedAt == null)
                .ToListAsync(cancellationToken);
            foreach (var refreshToken in activeRefreshTokens)
            {
                refreshToken.RevokedAt = DateTimeOffset.UtcNow;
            }

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(AuthLogMessages.PasswordResetCompleted, user.Id);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
