using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Rewards;

public static class SetRewardActive
{
    // RewardId comes from the route, not the request body — same convention as UpdateReward.
    public record RequestBody(bool IsActive);

    public record Command(Guid RewardId, bool IsActive) : IRequest<Result<RewardResponse>>;

    public class Handler(IAppDbContext db, IUserContext userContext, IStringLocalizer<SharedResources> localizer, ILogger<Handler> logger)
        : IRequestHandler<Command, Result<RewardResponse>>
    {
        public async Task<Result<RewardResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            var reward = await db.Rewards.SingleOrDefaultAsync(r => r.Id == command.RewardId, cancellationToken);
            if (reward is null)
            {
                return Result<RewardResponse>.Failure(new Error(
                    RewardsMessageKeys.NotFound,
                    localizer[RewardsMessageKeys.NotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            var business = await db.Businesses.SingleAsync(b => b.Id == reward.BusinessId, cancellationToken);
            if (business.OwnerUserId != userContext.UserId)
            {
                return Result<RewardResponse>.Failure(new Error(
                    RewardsMessageKeys.NotYourBusiness,
                    localizer[RewardsMessageKeys.NotYourBusiness].Value,
                    StatusCodes.Status403Forbidden));
            }

            reward.IsActive = command.IsActive;
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(RewardsLogMessages.RewardActiveStateChanged, reward.Id, reward.IsActive, userContext.UserId);

            return Result<RewardResponse>.Success(new RewardResponse(
                reward.Id, reward.BusinessId, reward.Title, reward.Description, reward.CrystalCost, reward.IsActive, reward.CreatedAt));
        }
    }
}
