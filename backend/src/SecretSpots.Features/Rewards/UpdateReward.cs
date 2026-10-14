using FluentValidation;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Rewards;

public static class UpdateReward
{
    // RewardId comes from the route, not the request body — kept as a separate
    // record so the endpoint can bind RequestBody from JSON and Command from both.
    public record RequestBody(string Title, string Description, int CrystalCost);

    public record Command(
        Guid RewardId,
        string Title,
        string Description,
        int CrystalCost) : IRequest<Result<RewardResponse>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IStringLocalizer<SharedResources> localizer)
        {
            this.ValidateRewardFields(c => c.Title, c => c.Description, c => c.CrystalCost, localizer);
        }
    }

    public class Handler(IAppDbContext db, IUserContext userContext, IStringLocalizer<SharedResources> localizer, ILogger<Handler> logger)
        : IRequestHandler<Command, Result<RewardResponse>>
    {
        public async Task<Result<RewardResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            var rewardResult = await RewardOwnershipHandler.GetOwnedRewardAsync(db, userContext, localizer, command.RewardId, cancellationToken);
            if (!rewardResult.IsSuccess)
            {
                return Result<RewardResponse>.Failure(rewardResult.Error);
            }
            var reward = rewardResult.Value;

            reward.Title = command.Title.Trim();
            reward.Description = command.Description.Trim();
            reward.CrystalCost = command.CrystalCost;

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(RewardsLogMessages.RewardUpdated, reward.Id, userContext.UserId);

            return Result<RewardResponse>.Success(new RewardResponse(
                reward.Id, reward.BusinessId, reward.Title, reward.Description, reward.CrystalCost, reward.IsActive, reward.CreatedAt));
        }
    }
}
