using Microsoft.EntityFrameworkCore;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.SavedSpots;

public static class UnsaveSpot
{
    public record Command(Guid SpotId) : IRequest<Result<Unit>>;

    public class Handler(IAppDbContext db, IUserContext userContext) : IRequestHandler<Command, Result<Unit>>
    {
        public async Task<Result<Unit>> Handle(Command command, CancellationToken cancellationToken)
        {
            var savedSpot = await db.SavedSpots.SingleOrDefaultAsync(
                s => s.SpotId == command.SpotId && s.UserId == userContext.UserId, cancellationToken);

            if (savedSpot is not null)
            {
                db.SavedSpots.Remove(savedSpot);
                await db.SaveChangesAsync(cancellationToken);
            }

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
