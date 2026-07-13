using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecretSpots.Domain;
using SecretSpots.Features.Auth;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Rewards;

public static class RedeemReward
{
    public record Command(Guid RewardId) : IRequest<Result<RewardRedemptionResponse>>;

    public class Handler(IAppDbContext db, IUserContext userContext, IStringLocalizer<SharedResources> localizer, ILogger<Handler> logger)
        : IRequestHandler<Command, Result<RewardRedemptionResponse>>
    {
        public async Task<Result<RewardRedemptionResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            var reward = await db.Rewards.SingleOrDefaultAsync(r => r.Id == command.RewardId, cancellationToken);
            if (reward is null)
            {
                return Result<RewardRedemptionResponse>.Failure(new Error(
                    RewardsMessageKeys.NotFound,
                    localizer[RewardsMessageKeys.NotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);
            if (user is null)
            {
                return Result<RewardRedemptionResponse>.Failure(new Error(
                    AuthMessageKeys.UserNotFound,
                    localizer[AuthMessageKeys.UserNotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            if (user.CrystalBalance < reward.CrystalCost)
            {
                return Result<RewardRedemptionResponse>.Failure(new Error(
                    RewardsMessageKeys.InsufficientBalance,
                    localizer[RewardsMessageKeys.InsufficientBalance].Value,
                    StatusCodes.Status400BadRequest));
            }

            user.CrystalBalance -= reward.CrystalCost;

            var redemption = new RewardRedemption
            {
                Id = Guid.NewGuid(),
                RewardId = reward.Id,
                BusinessId = reward.BusinessId,
                UserId = user.Id,
                CrystalsSpent = reward.CrystalCost,
            };

            db.RewardRedemptions.Add(redemption);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(RewardsLogMessages.RewardRedeemed, reward.Id, user.Id, redemption.CrystalsSpent);

            return Result<RewardRedemptionResponse>.Success(new RewardRedemptionResponse(
                redemption.Id, reward.Id, redemption.CrystalsSpent, user.CrystalBalance, redemption.CreatedAt));
        }
    }
}
