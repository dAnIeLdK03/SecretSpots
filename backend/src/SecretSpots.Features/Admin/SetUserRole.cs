using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecretSpots.Domain;
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

    public class Handler(IAppDbContext db, IUserContext userContext, IStringLocalizer<SharedResources> localizer, ILogger<Handler> logger)
        : IRequestHandler<Command, Result<AdminUserResponse>>
    {
        public async Task<Result<AdminUserResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            var user = await db.Users.SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);
            if (user is null)
            {
                return Result<AdminUserResponse>.Failure(Error.NotFound(AdminMessageKeys.UserNotFound, localizer));
            }

            user.Role = command.Role;
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(AdminLogMessages.UserRoleChanged, user.Id, user.Role, userContext.UserId);

            return Result<AdminUserResponse>.Success(new AdminUserResponse(user.Id, user.Email, user.DisplayName, user.Role));
        }
    }
}
