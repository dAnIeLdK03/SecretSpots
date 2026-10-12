using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Common.Validation;

namespace SecretSpots.Features.Rewards;

public static class GetMyRedemptions
{
    public record Query(int Page, int PageSize) : IRequest<PagedResponse<MyRedemptionResponse>>;

    public class Validator : AbstractValidator<Query>
    {
        public Validator(IStringLocalizer<SharedResources> localizer, IOptions<RewardsOptions> rewardsOptions)
        {
            this.ValidatePagination(
                q => q.Page,
                q => q.PageSize,
                rewardsOptions.Value.MaxPageSize,
                localizer[RewardsMessageKeys.PageOutOfRange].Value,
                localizer[RewardsMessageKeys.PageSizeOutOfRange].Value);
        }
    }

    public class Handler(IAppDbContext db, IUserContext userContext) : IRequestHandler<Query, PagedResponse<MyRedemptionResponse>>
    {
        public async Task<PagedResponse<MyRedemptionResponse>> Handle(Query query, CancellationToken cancellationToken)
        {
            // RewardTitle/BusinessName are denormalized onto RewardRedemption at redeem time, so
            // this reads straight off it — no join needed, and it keeps returning correct history
            // even after the reward or business behind a redemption has since been deleted.
            var baseQuery = db.RewardRedemptions.Where(r => r.UserId == userContext.UserId);

            return await baseQuery
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new MyRedemptionResponse(
                    r.Id,
                    r.RewardId,
                    r.RewardTitle,
                    r.BusinessId,
                    r.BusinessName,
                    r.CrystalsSpent,
                    r.CreatedAt))
                .ToPagedResponseAsync(query.Page, query.PageSize, cancellationToken);
        }
    }
}
