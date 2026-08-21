using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Rewards;

public static class GetMyRedemptions
{
    public record Query(int Page, int PageSize) : IRequest<RedemptionsPageResponse>;

    public class Validator : AbstractValidator<Query>
    {
        public Validator(IStringLocalizer<SharedResources> localizer, IOptions<RewardsOptions> rewardsOptions)
        {
            RuleFor(q => q.Page)
                .GreaterThanOrEqualTo(1).WithMessage(localizer[RewardsMessageKeys.PageOutOfRange].Value);

            RuleFor(q => q.PageSize)
                .InclusiveBetween(1, rewardsOptions.Value.MaxPageSize)
                    .WithMessage(localizer[RewardsMessageKeys.PageSizeOutOfRange].Value);
        }
    }

    public class Handler(IAppDbContext db, IUserContext userContext) : IRequestHandler<Query, RedemptionsPageResponse>
    {
        public async Task<RedemptionsPageResponse> Handle(Query query, CancellationToken cancellationToken)
        {
            // Safe as inner joins: DeleteReward now deletes a reward's redemptions along with it
            // (no dangling RewardId), and businesses currently have no delete feature at all.
            var baseQuery =
                from redemption in db.RewardRedemptions
                join reward in db.Rewards on redemption.RewardId equals reward.Id
                join business in db.Businesses on redemption.BusinessId equals business.Id
                where redemption.UserId == userContext.UserId
                select new { redemption, reward.Title, business.Name };

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var items = await baseQuery
                .OrderByDescending(x => x.redemption.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(x => new MyRedemptionResponse(
                    x.redemption.Id,
                    x.redemption.RewardId,
                    x.Title,
                    x.redemption.BusinessId,
                    x.Name,
                    x.redemption.CrystalsSpent,
                    x.redemption.CreatedAt))
                .ToListAsync(cancellationToken);

            return new RedemptionsPageResponse(items, query.Page, query.PageSize, totalCount);
        }
    }
}
