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

namespace SecretSpots.Features.Rewards;

public static class GetBusinessRewards
{
    public record Query(Guid BusinessId, int Page, int PageSize) : IRequest<Result<RewardsPageResponse>>;

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

    public class Handler(IAppDbContext db, IStringLocalizer<SharedResources> localizer)
        : IRequestHandler<Query, Result<RewardsPageResponse>>
    {
        public async Task<Result<RewardsPageResponse>> Handle(Query query, CancellationToken cancellationToken)
        {
            var businessExists = await db.Businesses.AnyAsync(b => b.Id == query.BusinessId, cancellationToken);
            if (!businessExists)
            {
                return Result<RewardsPageResponse>.Failure(new Error(
                    BusinessesMessageKeys.NotFound,
                    localizer[BusinessesMessageKeys.NotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            var baseQuery = db.Rewards.Where(r => r.BusinessId == query.BusinessId);

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var items = await baseQuery
                .OrderBy(r => r.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(r => new RewardResponse(r.Id, r.BusinessId, r.Title, r.Description, r.CrystalCost, r.CreatedAt))
                .ToListAsync(cancellationToken);

            return Result<RewardsPageResponse>.Success(new RewardsPageResponse(items, query.Page, query.PageSize, totalCount));
        }
    }
}
