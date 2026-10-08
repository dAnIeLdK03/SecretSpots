using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecretSpots.Domain;
using SecretSpots.Features.Common.ExceptionHandling;
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
                return Result<RewardRedemptionResponse>.Failure(Error.NotFound(RewardsMessageKeys.NotFound, localizer));
            }

            if (!reward.IsActive)
            {
                return Result<RewardRedemptionResponse>.Failure(new Error(
                    RewardsMessageKeys.RewardInactive,
                    localizer[RewardsMessageKeys.RewardInactive].Value,
                    StatusCodes.Status400BadRequest));
            }

            // Reward always has a valid BusinessId (CreateReward requires the business to exist,
            // and nothing lets one outlive its business — BusinessDeletionCleanup deletes rewards
            // along with it), so this is safe as a SingleAsync rather than a null-checked lookup.
            var business = await db.Businesses.SingleAsync(b => b.Id == reward.BusinessId, cancellationToken);

            // A paused business is excluded from SearchNearbyBusinesses precisely so it stops
            // getting new business — checking only reward.IsActive above would let that be
            // bypassed via any bookmarked/cached/linked business or reward page, producing
            // redemptions the (deliberately unavailable) business can't actually fulfill.
            if (!business.IsActive)
            {
                return Result<RewardRedemptionResponse>.Failure(new Error(
                    RewardsMessageKeys.BusinessInactive,
                    localizer[RewardsMessageKeys.BusinessInactive].Value,
                    StatusCodes.Status400BadRequest));
            }

            var wallet = await db.CrystalWallets.SingleOrDefaultAsync(w => w.UserId == userContext.UserId, cancellationToken);
            if (wallet is null)
            {
                return Result<RewardRedemptionResponse>.Failure(Error.NotFound(CommonMessageKeys.CurrentUserNotFound, localizer));
            }

            if (wallet.Balance < reward.CrystalCost)
            {
                return Result<RewardRedemptionResponse>.Failure(new Error(
                    RewardsMessageKeys.InsufficientBalance,
                    localizer[RewardsMessageKeys.InsufficientBalance].Value,
                    StatusCodes.Status400BadRequest));
            }

            wallet.Balance -= reward.CrystalCost;

            var redemption = new RewardRedemption
            {
                Id = Guid.NewGuid(),
                RewardId = reward.Id,
                BusinessId = reward.BusinessId,
                UserId = userContext.UserId,
                CrystalsSpent = reward.CrystalCost,
                RewardTitle = reward.Title,
                BusinessName = business.Name,
            };

            var crystalTransaction = new CrystalTransaction
            {
                Id = Guid.NewGuid(),
                UserId = userContext.UserId,
                Amount = -reward.CrystalCost,
                Reason = CrystalTransactionReason.RewardRedemption,
                RelatedRedemptionId = redemption.Id,
            };

            db.RewardRedemptions.Add(redemption);
            db.CrystalTransactions.Add(crystalTransaction);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                // The balance check above raced with another update to this same user (another
                // redemption, a check-in) — reject rather than silently spend a stale balance.
                return Result<RewardRedemptionResponse>.Failure(new Error(
                    CommonMessageKeys.ConcurrencyConflict,
                    localizer[CommonMessageKeys.ConcurrencyConflict].Value,
                    StatusCodes.Status409Conflict));
            }

            logger.LogInformation(RewardsLogMessages.RewardRedeemed, reward.Id, userContext.UserId, redemption.CrystalsSpent);

            return Result<RewardRedemptionResponse>.Success(new RewardRedemptionResponse(
                redemption.Id, reward.Id, redemption.CrystalsSpent, wallet.Balance, redemption.CreatedAt));
        }
    }
}
