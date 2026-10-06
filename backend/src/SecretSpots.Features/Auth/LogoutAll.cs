using Microsoft.EntityFrameworkCore;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Auth;

public static class LogoutAll
{
    public record Command : IRequest<Result<Unit>>;

    public class Handler(IAppDbContext db, IUserContext userContext) : IRequestHandler<Command, Result<Unit>>
    {
        public async Task<Result<Unit>> Handle(Command command, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            await db.RefreshTokens
                .Where(t => t.UserId == userContext.UserId && t.RevokedAt == null)
                .ExecuteUpdateAsync(t => t.SetProperty(x => x.RevokedAt, now), cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
