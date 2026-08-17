using Microsoft.EntityFrameworkCore;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;

namespace SecretSpots.Features.Auth;

public static class Logout
{
    public record Command(string RefreshToken) : IRequest<Result<Unit>>;

    public class Handler(IAppDbContext db) : IRequestHandler<Command, Result<Unit>>
    {
        public async Task<Result<Unit>> Handle(Command command, CancellationToken cancellationToken)
        {
            var existingToken = string.IsNullOrEmpty(command.RefreshToken)
                ? null
                : await db.RefreshTokens.SingleOrDefaultAsync(t => t.Token == command.RefreshToken, cancellationToken);

            if (existingToken is { RevokedAt: null })
            {
                existingToken.RevokedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
