using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Businesses;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Validation;

namespace SecretSpots.Features.Rewards;

public static class GetBusinessRewards
{
    public record Query(Guid BusinessId, int Page, int PageSize) : IRequest<Result<PagedResponse<RewardResponse>>>;

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

    public class Handler(IAppDbContext db, IStringLocalizer<SharedResources> localizer)
        : IRequestHandler<Query, Result<PagedResponse<RewardResponse>>>
    {
        public async Task<Result<PagedResponse<RewardResponse>>> Handle(Query query, CancellationToken cancellationToken)
        {
            var businessExists = await db.Businesses.AnyAsync(b => b.Id == query.BusinessId, cancellationToken);
            if (!businessExists)
            {
                return Result<PagedResponse<RewardResponse>>.Failure(Error.NotFound(BusinessesMessageKeys.NotFound, localizer));
            }

            var baseQuery = db.Rewards.Where(r => r.BusinessId == query.BusinessId);

            var pagedResponse = await baseQuery
                .OrderBy(r => r.CreatedAt)
                .Select(r => new RewardResponse(r.Id, r.BusinessId, r.Title, r.Description, r.CrystalCost, r.IsActive, r.CreatedAt))
                .ToPagedResponseAsync(query.Page, query.PageSize, cancellationToken);

            return Result<PagedResponse<RewardResponse>>.Success(pagedResponse);
        }
    }
}
