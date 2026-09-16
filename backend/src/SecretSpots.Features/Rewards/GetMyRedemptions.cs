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
            // RewardTitle/BusinessName are denormalized onto RewardRedemption at redeem time, so
            // this reads straight off it — no join needed, and it keeps returning correct history
            // even after the reward or business behind a redemption has since been deleted.
            var baseQuery = db.RewardRedemptions.Where(r => r.UserId == userContext.UserId);

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var items = await baseQuery
                .OrderByDescending(r => r.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(r => new MyRedemptionResponse(
                    r.Id,
                    r.RewardId,
                    r.RewardTitle,
                    r.BusinessId,
                    r.BusinessName,
                    r.CrystalsSpent,
                    r.CreatedAt))
                .ToListAsync(cancellationToken);

            return new RedemptionsPageResponse(items, query.Page, query.PageSize, totalCount);
        }
    }
}
