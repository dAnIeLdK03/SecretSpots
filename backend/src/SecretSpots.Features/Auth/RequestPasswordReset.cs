using System.Security.Cryptography;
using FluentValidation;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Email;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;

namespace SecretSpots.Features.Auth;

public static class RequestPasswordReset
{
    public record Command(string Email) : IRequest<Result<Unit>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(c => c.Email)
                .NotEmpty().WithMessage(localizer[AuthMessageKeys.EmailRequired].Value)
                .EmailAddress().WithMessage(localizer[AuthMessageKeys.EmailInvalidFormat].Value);
        }
    }

    public class Handler(
        IAppDbContext db,
        IEmailSender emailSender,
        IOptions<PasswordResetOptions> passwordResetOptions,
        IStringLocalizer<SharedResources> localizer,
        ILogger<Handler> logger)
        : IRequestHandler<Command, Result<Unit>>
    {
        public async Task<Result<Unit>> Handle(Command command, CancellationToken cancellationToken)
        {
            var normalizedEmail = command.Email.Trim().ToLowerInvariant();

            var user = await db.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

            // Never reveal whether the email is registered — same response either way. If there's
            // no matching (password-based) account, silently do nothing past this point.
            if (user is null || user.PasswordHash is null)
            {
                logger.LogInformation(AuthLogMessages.PasswordResetRequestedForUnknownEmail, normalizedEmail);
                return Result<Unit>.Success(Unit.Value);
            }

            var token = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                    .Replace('+', '-').Replace('/', '_').TrimEnd('='),
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(passwordResetOptions.Value.TokenExpiryMinutes),
            };

            db.PasswordResetTokens.Add(token);
            await db.SaveChangesAsync(cancellationToken);

            var resetLink = QueryHelpers.AddQueryString(
                passwordResetOptions.Value.FrontendResetUrl, "token", token.Token);

            await emailSender.SendAsync(
                user.Email,
                localizer[AuthMessageKeys.PasswordResetEmailSubject].Value,
                string.Format(localizer[AuthMessageKeys.PasswordResetEmailBody].Value, resetLink),
                cancellationToken);

            logger.LogInformation(AuthLogMessages.PasswordResetRequested, user.Id);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
