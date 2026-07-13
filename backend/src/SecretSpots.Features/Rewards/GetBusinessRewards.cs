using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SecretSpots.Features.Businesses;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;

namespace SecretSpots.Features.Rewards;

public static class GetBusinessRewards
{
    public record Query(Guid BusinessId) : IRequest<Result<IReadOnlyList<RewardResponse>>>;

    public class Handler(IAppDbContext db, IStringLocalizer<SharedResources> localizer)
        : IRequestHandler<Query, Result<IReadOnlyList<RewardResponse>>>
    {
        public async Task<Result<IReadOnlyList<RewardResponse>>> Handle(Query query, CancellationToken cancellationToken)
        {
            var businessExists = await db.Businesses.AnyAsync(b => b.Id == query.BusinessId, cancellationToken);
            if (!businessExists)
            {
                return Result<IReadOnlyList<RewardResponse>>.Failure(new Error(
                    BusinessesMessageKeys.NotFound,
                    localizer[BusinessesMessageKeys.NotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            var rewards = await db.Rewards
                .Where(r => r.BusinessId == query.BusinessId)
                .OrderBy(r => r.CreatedAt)
                .Select(r => new RewardResponse(r.Id, r.BusinessId, r.Title, r.Description, r.CrystalCost, r.CreatedAt))
                .ToListAsync(cancellationToken);

            return Result<IReadOnlyList<RewardResponse>>.Success(rewards);
        }
    }
}
