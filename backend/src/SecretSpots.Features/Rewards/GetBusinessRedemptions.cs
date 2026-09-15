using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Businesses;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Rewards;

public static class GetBusinessRedemptions
{
    public record Query(Guid BusinessId, int Page, int PageSize) : IRequest<Result<BusinessRedemptionsPageResponse>>;

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

    public class Handler(IAppDbContext db, IUserContext userContext, IStringLocalizer<SharedResources> localizer)
        : IRequestHandler<Query, Result<BusinessRedemptionsPageResponse>>
    {
        public async Task<Result<BusinessRedemptionsPageResponse>> Handle(Query query, CancellationToken cancellationToken)
        {
            var business = await db.Businesses.SingleOrDefaultAsync(b => b.Id == query.BusinessId, cancellationToken);
            if (business is null)
            {
                return Result<BusinessRedemptionsPageResponse>.Failure(new Error(
                    BusinessesMessageKeys.NotFound,
                    localizer[BusinessesMessageKeys.NotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            if (business.OwnerUserId != userContext.UserId)
            {
                return Result<BusinessRedemptionsPageResponse>.Failure(new Error(
                    RewardsMessageKeys.NotYourBusiness,
                    localizer[RewardsMessageKeys.NotYourBusiness].Value,
                    StatusCodes.Status403Forbidden));
            }

            // Safe as inner joins: DeleteReward deletes a reward's redemptions along with it (no
            // dangling RewardId), and a User row is never deleted independently of its
            // redemptions (DeleteAccount would need equivalent cleanup, which is out of scope
            // here — same assumption GetMyRedemptions already makes for Reward/Business).
            var baseQuery =
                from redemption in db.RewardRedemptions
                join reward in db.Rewards on redemption.RewardId equals reward.Id
                join user in db.Users on redemption.UserId equals user.Id
                where redemption.BusinessId == query.BusinessId
                select new { redemption, reward.Title, user.DisplayName };

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var items = await baseQuery
                // Unfulfilled first — that's the queue staff actually needs to work through.
                .OrderBy(x => x.redemption.FulfilledAt != null)
                .ThenByDescending(x => x.redemption.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(x => new BusinessRedemptionResponse(
                    x.redemption.Id,
                    x.redemption.RewardId,
                    x.Title,
                    x.DisplayName,
                    x.redemption.CrystalsSpent,
                    x.redemption.RedemptionCode,
                    x.redemption.FulfilledAt != null,
                    x.redemption.FulfilledAt,
                    x.redemption.CreatedAt))
                .ToListAsync(cancellationToken);

            return Result<BusinessRedemptionsPageResponse>.Success(
                new BusinessRedemptionsPageResponse(items, query.Page, query.PageSize, totalCount));
        }
    }
}
