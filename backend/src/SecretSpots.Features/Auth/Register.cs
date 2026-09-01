using System.Net.Mail;
using FluentValidation;
using Microsoft.AspNetCore.Http;
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
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Auth;

public static class Register
{
    public record Command(string Email, string Password, string DisplayName) : IRequest<Result<AuthResult>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(c => c.Email)
                .NotEmpty().WithMessage(localizer[AuthMessageKeys.EmailRequired].Value)
                .EmailAddress().WithMessage(localizer[AuthMessageKeys.EmailInvalidFormat].Value);

            RuleFor(c => c.Password)
                .NotEmpty().WithMessage(localizer[AuthMessageKeys.PasswordRequired].Value)
                .MinimumLength(8).WithMessage(localizer[AuthMessageKeys.PasswordTooShort].Value)
                .Must(PasswordRules.ContainUpperCase).WithMessage(localizer[AuthMessageKeys.PasswordRequiresUpper].Value)
                .Must(PasswordRules.ContainLowerCase).WithMessage(localizer[AuthMessageKeys.PasswordRequiresLower].Value)
                .Must(PasswordRules.ContainDigit).WithMessage(localizer[AuthMessageKeys.PasswordRequiresDigit].Value)
                .Must(PasswordRules.NotBeCommonPassword).WithMessage(localizer[AuthMessageKeys.PasswordIsCommon].Value);

            RuleFor(c => c.DisplayName)
                .NotEmpty().WithMessage(localizer[AuthMessageKeys.DisplayNameRequired].Value)
                .MaximumLength(50).WithMessage(localizer[AuthMessageKeys.DisplayNameTooLong].Value);
        }
    }

    public class Handler(
        IAppDbContext db,
        IJwtService jwtService,
        IOptions<JwtOptions> jwtOptions,
        IOptions<CrystalsOptions> crystalsOptions,
        IEmailSender emailSender,
        IOptions<EmailVerificationOptions> emailVerificationOptions,
        IStringLocalizer<SharedResources> localizer,
        ILogger<Handler> logger)
        : IRequestHandler<Command, Result<AuthResult>>
    {
        public async Task<Result<AuthResult>> Handle(Command command, CancellationToken cancellationToken)
        {
            var normalizedEmail = command.Email.Trim().ToLowerInvariant();

            // The Validator already checks this for callers going through the mediator —
            // this re-checks it so the Handler never persists a malformed email even if
            // it were ever invoked directly, bypassing the validation pipeline.
            if (!MailAddress.TryCreate(normalizedEmail, out _))
            {
                return Result<AuthResult>.Failure(new Error(
                    AuthMessageKeys.EmailInvalidFormat,
                    localizer[AuthMessageKeys.EmailInvalidFormat].Value,
                    StatusCodes.Status400BadRequest));
            }

            var emailTaken = await db.Users
                .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

            if (emailTaken)
            {
                return Result<AuthResult>.Failure(new Error(
                    AuthMessageKeys.EmailAlreadyRegistered,
                    localizer[AuthMessageKeys.EmailAlreadyRegistered].Value,
                    StatusCodes.Status409Conflict));
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(command.Password, workFactor: 12),
                DisplayName = command.DisplayName.Trim(),
                CrystalBalance = crystalsOptions.Value.StartingBalance,
            };

            await using var transaction = await db.BeginTransactionAsync(cancellationToken);
            db.Users.Add(user);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                return Result<AuthResult>.Failure(new Error(
                    AuthMessageKeys.EmailAlreadyRegistered,
                    localizer[AuthMessageKeys.EmailAlreadyRegistered].Value,
                    StatusCodes.Status409Conflict));
            }

            var issueResult = await AuthTokenIssuer.IssueAsync(db, jwtService, jwtOptions, user, rememberMe: true, cancellationToken);
            if (!issueResult.IsSuccess)
            {
                return issueResult;
            }

            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(AuthLogMessages.UserRegistered, user.Id, user.Email);

            // Best-effort — the account and tokens are already committed at this point, so a
            // flaky email provider must not turn into a 500 on an otherwise-successful
            // registration. The user can always ask for a resend once logged in.
            try
            {
                await EmailVerificationSender.SendAsync(db, emailSender, emailVerificationOptions, localizer, user, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, AuthLogMessages.EmailVerificationSendFailedAfterRegister, user.Id);
            }

            return issueResult;
        }
    }
}