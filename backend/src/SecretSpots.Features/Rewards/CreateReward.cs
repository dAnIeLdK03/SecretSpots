using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecretSpots.Domain;
using SecretSpots.Features.Businesses;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Rewards;

public static class CreateReward
{
    // BusinessId comes from the route, not the request body — kept as a separate
    // record so the endpoint can bind RequestBody from JSON and Command from both.
    public record RequestBody(string Title, string Description, int CrystalCost);

    public record Command(
        Guid BusinessId,
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
            var business = await db.Businesses.SingleOrDefaultAsync(b => b.Id == command.BusinessId, cancellationToken);
            if (business is null)
            {
                return Result<RewardResponse>.Failure(new Error(
                    BusinessesMessageKeys.NotFound,
                    localizer[BusinessesMessageKeys.NotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            if (business.OwnerUserId != userContext.UserId)
            {
                return Result<RewardResponse>.Failure(new Error(
                    RewardsMessageKeys.NotYourBusiness,
                    localizer[RewardsMessageKeys.NotYourBusiness].Value,
                    StatusCodes.Status403Forbidden));
            }

            var reward = new Reward
            {
                Id = Guid.NewGuid(),
                BusinessId = business.Id,
                Title = command.Title.Trim(),
                Description = command.Description.Trim(),
                CrystalCost = command.CrystalCost,
            };

            db.Rewards.Add(reward);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(RewardsLogMessages.RewardCreated, reward.Id, business.Id, userContext.UserId);

            return Result<RewardResponse>.Success(new RewardResponse(
                reward.Id, reward.BusinessId, reward.Title, reward.Description, reward.CrystalCost, reward.CreatedAt));
        }
    }
}
