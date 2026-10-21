using FluentValidation;
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

namespace SecretSpots.Features.Admin;

public static class SetUserRole
{
    // UserId comes from the route, not the request body — same convention as other
    // admin-route commands (e.g. SetRewardActive).
    public record RequestBody(UserRole Role);

    public record Command(Guid UserId, UserRole Role) : IRequest<Result<AdminUserResponse>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(c => c.Role)
                .IsInEnum().WithMessage(localizer[AdminMessageKeys.RoleInvalid].Value);
        }
    }

    public class Handler(
        IAppDbContext db,
        IUserContext userContext,
        IEmailSender emailSender,
        IOptions<AdminSecurityOptions> adminSecurityOptions,
        IStringLocalizer<SharedResources> localizer,
        ILogger<Handler> logger)
        : IRequestHandler<Command, Result<AdminUserResponse>>
    {
        public async Task<Result<AdminUserResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            if (command.UserId == userContext.UserId)
            {
                return Result<AdminUserResponse>.Failure(Error.Forbidden(AdminMessageKeys.CannotChangeOwnRole, localizer));
            }

            var user = await db.Users.SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);
            if (user is null)
            {
                return Result<AdminUserResponse>.Failure(Error.NotFound(AdminMessageKeys.UserNotFound, localizer));
            }

            var wasPromotedToAdmin = user.Role != UserRole.Admin && command.Role == UserRole.Admin;

            user.Role = command.Role;
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(AdminLogMessages.UserRoleChanged, user.Id, user.Role, userContext.UserId);

            // Only the security contact is notified here, not the promoted user — the point is to
            // alert the project owner to a promotion they might not have made themselves, not to
            // announce it to the account being promoted. Best-effort: the role change above is
            // already committed, so a flaky email provider must not turn this into a 500.
            if (wasPromotedToAdmin)
            {
                try
                {
                    await emailSender.SendAsync(
                        adminSecurityOptions.Value.SecurityContactEmail,
                        localizer[AdminMessageKeys.RolePromotedToAdminEmailSubject].Value,
                        string.Format(
                            localizer[AdminMessageKeys.RolePromotedToAdminEmailBody].Value,
                            user.Email,
                            user.Id,
                            userContext.UserId),
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, AdminLogMessages.RolePromotionEmailSendFailed, user.Id);
                }
            }

            return Result<AdminUserResponse>.Success(new AdminUserResponse(user.Id, user.Email, user.DisplayName, user.Role));
        }
    }
}
