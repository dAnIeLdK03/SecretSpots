using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Auth;

public static class GetCurrentUser
{
    public record Query : IRequest<Result<Response>>;

    public record Response(Guid Id, string Email, string DisplayName, int CrystalBalance, bool IsEmailVerified, bool IsAdmin);

    public class Handler(
        IAppDbContext db,
        IUserContext userContext,
        IStringLocalizer<SharedResources> localizer,
        ILogger<Handler> logger)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken cancellationToken)
        {
            var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

            if (user is null)
            {
                return Result<Response>.Failure(Error.NotFound(AuthMessageKeys.UserNotFound, localizer));
            }

            // Wallets are created alongside every user (Register/ExternalAuthCallback), so a
            // missing one only happens for a pre-existing account the backfill missed.
            var wallet = await db.CrystalWallets.SingleOrDefaultAsync(w => w.UserId == user.Id, cancellationToken);

            logger.LogInformation(AuthLogMessages.UserProfileRetrieved, user.Id);

            return Result<Response>.Success(
                new Response(user.Id, user.Email, user.DisplayName, wallet?.Balance ?? 0, user.IsEmailVerified, user.IsAdmin));
        }
    }
}
