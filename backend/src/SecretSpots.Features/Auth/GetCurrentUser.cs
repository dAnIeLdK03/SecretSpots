using Microsoft.AspNetCore.Http;
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

    public record Response(Guid Id, string Email, string DisplayName, int CrystalBalance);

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
                return Result<Response>.Failure(new Error(
                    AuthMessageKeys.UserNotFound,
                    localizer[AuthMessageKeys.UserNotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            logger.LogInformation(AuthLogMessages.UserProfileRetrieved, user.Id);

            return Result<Response>.Success(new Response(user.Id, user.Email, user.DisplayName, user.CrystalBalance));
        }
    }
}
