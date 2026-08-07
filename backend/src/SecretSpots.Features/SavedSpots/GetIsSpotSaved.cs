using Microsoft.EntityFrameworkCore;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.SavedSpots;

public static class GetIsSpotSaved
{
    public record Query(Guid SpotId) : IRequest<bool>;

    public class Handler(IAppDbContext db, IUserContext userContext) : IRequestHandler<Query, bool>
    {
        public Task<bool> Handle(Query query, CancellationToken cancellationToken) =>
            db.SavedSpots.AnyAsync(s => s.SpotId == query.SpotId && s.UserId == userContext.UserId, cancellationToken);
    }
}
