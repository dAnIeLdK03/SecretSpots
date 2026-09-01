using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Email;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Auth;

public static class ResendEmailVerification
{
    public record Command : IRequest<Result<Unit>>;

    public class Handler(
        IAppDbContext db,
        IUserContext userContext,
        IEmailSender emailSender,
        IOptions<EmailVerificationOptions> emailVerificationOptions,
        IStringLocalizer<SharedResources> localizer,
        ILogger<Handler> logger)
        : IRequestHandler<Command, Result<Unit>>
    {
        public async Task<Result<Unit>> Handle(Command command, CancellationToken cancellationToken)
        {
            var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);
            if (user is null)
            {
                return Result<Unit>.Failure(new Error(
                    AuthMessageKeys.UserNotFound,
                    localizer[AuthMessageKeys.UserNotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            // Already verified — nothing to resend. The authenticated caller already knows their
            // own status from /auth/me, so there's no enumeration concern in saying so plainly.
            if (user.IsEmailVerified)
            {
                return Result<Unit>.Success(Unit.Value);
            }

            await EmailVerificationSender.SendAsync(db, emailSender, emailVerificationOptions, localizer, user, cancellationToken);

            logger.LogInformation(AuthLogMessages.EmailVerificationSent, user.Id);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
