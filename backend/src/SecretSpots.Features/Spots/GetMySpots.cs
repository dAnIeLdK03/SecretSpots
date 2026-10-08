using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Spots;

public static class GetMySpots
{
    public record Query(int Page, int PageSize) : IRequest<SpotSearchPageResponse>;

    public class Validator : AbstractValidator<Query>
    {
        public Validator(IStringLocalizer<SharedResources> localizer, IOptions<SpotSearchOptions> spotSearchOptions)
        {
            RuleFor(q => q.Page)
                .GreaterThanOrEqualTo(1).WithMessage(localizer[SpotsMessageKeys.PageOutOfRange].Value);

            RuleFor(q => q.PageSize)
                .InclusiveBetween(1, spotSearchOptions.Value.MaxPageSize)
                    .WithMessage(localizer[SpotsMessageKeys.PageSizeOutOfRange].Value);
        }
    }

    public class Handler(IAppDbContext db, IUserContext userContext) : IRequestHandler<Query, SpotSearchPageResponse>
    {
        public async Task<SpotSearchPageResponse> Handle(Query query, CancellationToken cancellationToken)
        {
            var baseQuery = db.Spots.Where(s => s.CreatedByUserId == userContext.UserId);

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var spots = await baseQuery
                .OrderByDescending(s => s.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            var items = spots
                .Select(s => new SpotSearchResultResponse(
                    s.Id,
                    s.Name,
                    s.Description,
                    s.Category,
                    s.PhotoUrls[0],
                    s.Location.Y,
                    s.Location.X,
                    s.CreatedByUserId,
                    s.CreatedAt))
                .ToList();

            return new SpotSearchPageResponse(items, query.Page, query.PageSize, totalCount);
        }
    }
}
