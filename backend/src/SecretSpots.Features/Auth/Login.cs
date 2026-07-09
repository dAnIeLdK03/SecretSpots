using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Auth;

public static class Login
{
    public record Command(string Email, string Password) : IRequest<Result<AuthResult>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(c => c.Email)
                .NotEmpty().WithMessage(localizer[AuthMessageKeys.EmailRequired].Value)
                .EmailAddress().WithMessage(localizer[AuthMessageKeys.EmailInvalidFormat].Value);

            RuleFor(c => c.Password)
                .NotEmpty().WithMessage(localizer[AuthMessageKeys.PasswordRequired].Value);
        }
    }

    public class Handler(
        IAppDbContext db,
        IJwtService jwtService,
        IOptions<JwtOptions> jwtOptions,
        IStringLocalizer<SharedResources> localizer,
        ILogger<Handler> logger)
        : IRequestHandler<Command, Result<AuthResult>>
    {
        public async Task<Result<AuthResult>> Handle(Command command, CancellationToken cancellationToken)
        {
            // Guards the Handler itself against a null/blank Email, the same way Register
            // does — model binding can hand us a null here despite the non-nullable type,
            // and this must run before .Trim() so it can't NullReferenceException instead.
            if (string.IsNullOrWhiteSpace(command.Email))
            {
                return Result<AuthResult>.Failure(new Error(
                    AuthMessageKeys.EmailRequired,
                    localizer[AuthMessageKeys.EmailRequired].Value,
                    StatusCodes.Status400BadRequest));
            }

            var normalizedEmail = command.Email.Trim().ToLowerInvariant();

            var user = await db.Users
                .SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

            // Same error for "no such user" and "wrong password" — do not let an attacker
            // learn which emails are registered.
            if (user is null || !BCrypt.Net.BCrypt.Verify(command.Password, user.PasswordHash))
            {
                logger.LogWarning(AuthLogMessages.FailedLoginAttempt, normalizedEmail);
                return Result<AuthResult>.Failure(new Error(
                    AuthMessageKeys.InvalidCredentials,
                    localizer[AuthMessageKeys.InvalidCredentials].Value,
                    StatusCodes.Status401Unauthorized));
            }

            var authResult = await AuthTokenIssuer.IssueAsync(db, jwtService, jwtOptions, user, cancellationToken);
            if (!authResult.IsSuccess)
            {
                return Result<AuthResult>.Failure(authResult.Error);
            }

            logger.LogInformation(AuthLogMessages.UserLoggedIn, user.Id);
            return Result<AuthResult>.Success(authResult.Value);
        }
    }
}
