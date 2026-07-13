using FluentValidation;
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
            RuleFor(c => c.Title)
                .NotEmpty().WithMessage(localizer[RewardsMessageKeys.TitleRequired].Value)
                .MaximumLength(100).WithMessage(localizer[RewardsMessageKeys.TitleTooLong].Value);

            RuleFor(c => c.Description)
                .NotEmpty().WithMessage(localizer[RewardsMessageKeys.DescriptionRequired].Value)
                .MaximumLength(2000).WithMessage(localizer[RewardsMessageKeys.DescriptionTooLong].Value);

            RuleFor(c => c.CrystalCost)
                .GreaterThan(0).WithMessage(localizer[RewardsMessageKeys.CrystalCostOutOfRange].Value);
        }
    }

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

            reward.Title = command.Title.Trim();
            reward.Description = command.Description.Trim();
            reward.CrystalCost = command.CrystalCost;

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(RewardsLogMessages.RewardUpdated, reward.Id, userContext.UserId);

            return Result<RewardResponse>.Success(new RewardResponse(
                reward.Id, reward.BusinessId, reward.Title, reward.Description, reward.CrystalCost, reward.CreatedAt));
        }
    }
}
