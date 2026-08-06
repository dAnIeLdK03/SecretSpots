using Microsoft.EntityFrameworkCore;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Ratings;

public static class GetMyRating
{
    public record Query(Guid SpotId) : IRequest<int?>;

    public class Handler(IAppDbContext db, IUserContext userContext) : IRequestHandler<Query, int?>
    {
        public async Task<int?> Handle(Query query, CancellationToken cancellationToken)
        {
            var rating = await db.Ratings.SingleOrDefaultAsync(
                r => r.SpotId == query.SpotId && r.UserId == userContext.UserId, cancellationToken);

            return rating?.Value;
        }
    }
}
