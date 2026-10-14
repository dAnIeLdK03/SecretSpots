using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Rewards;

internal static class RewardOwnershipHandler
{
    public static async Task<Result<Reward>> GetOwnedRewardAsync(
        IAppDbContext db,
        IUserContext userContext,
        IStringLocalizer<SharedResources> localizer,
        Guid rewardId,
        CancellationToken cancellationToken)
    {
        var reward = await db.Rewards.SingleOrDefaultAsync(r => r.Id == rewardId, cancellationToken);
        if (reward is null)
        {
            return Result<Reward>.Failure(Error.NotFound(RewardsMessageKeys.NotFound, localizer));
        }

        var business = await db.Businesses.SingleAsync(b => b.Id == reward.BusinessId, cancellationToken);
        if (business.OwnerUserId != userContext.UserId)
        {
            return Result<Reward>.Failure(Error.Forbidden(RewardsMessageKeys.NotYourBusiness, localizer));
        }

        return Result<Reward>.Success(reward);
    }
}
