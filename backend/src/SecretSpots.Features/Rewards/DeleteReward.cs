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

public static class DeleteReward
{
    public record Command(Guid RewardId) : IRequest<Result<Unit>>;

    public class Handler(IAppDbContext db, IUserContext userContext, IStringLocalizer<SharedResources> localizer, ILogger<Handler> logger)
        : IRequestHandler<Command, Result<Unit>>
    {
        public async Task<Result<Unit>> Handle(Command command, CancellationToken cancellationToken)
        {
            var reward = await db.Rewards.SingleOrDefaultAsync(r => r.Id == command.RewardId, cancellationToken);
            if (reward is null)
            {
                return Result<Unit>.Failure(new Error(
                    RewardsMessageKeys.NotFound,
                    localizer[RewardsMessageKeys.NotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            var business = await db.Businesses.SingleAsync(b => b.Id == reward.BusinessId, cancellationToken);
            if (business.OwnerUserId != userContext.UserId)
            {
                return Result<Unit>.Failure(new Error(
                    RewardsMessageKeys.NotYourBusiness,
                    localizer[RewardsMessageKeys.NotYourBusiness].Value,
                    StatusCodes.Status403Forbidden));
            }

            db.Rewards.Remove(reward);
            await db.SaveChangesAsync(cancellationToken);

            // Redemptions are deliberately left alone — RewardTitle/BusinessName are denormalized
            // onto RewardRedemption at redeem time precisely so a user's spend history keeps
            // reading correctly after the reward (or its business) is gone. RewardId above is left
            // dangling, same as every other FK-less reference in this app; nothing dereferences it
            // after the fact.
            logger.LogInformation(RewardsLogMessages.RewardDeleted, reward.Id, userContext.UserId);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
