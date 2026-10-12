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

namespace SecretSpots.Features.Spots;

public static class GetMySpots
{
    public record Query(int Page, int PageSize) : IRequest<PagedResponse<SpotSearchResultResponse>>;

    public class Validator : AbstractValidator<Query>
    {
        public Validator(IStringLocalizer<SharedResources> localizer, IOptions<SpotSearchOptions> spotSearchOptions)
        {
            this.ValidatePagination(
                q => q.Page,
                q => q.PageSize,
                spotSearchOptions.Value.MaxPageSize,
                localizer[SpotsMessageKeys.PageOutOfRange].Value,
                localizer[SpotsMessageKeys.PageSizeOutOfRange].Value);
        }
    }

    public class Handler(IAppDbContext db, IUserContext userContext) : IRequestHandler<Query, PagedResponse<SpotSearchResultResponse>>
    {
        public async Task<PagedResponse<SpotSearchResultResponse>> Handle(Query query, CancellationToken cancellationToken)
        {
            var baseQuery = db.Spots.Where(s => s.CreatedByUserId == userContext.UserId);

            // Materialized first, then shaped in-memory — Location.Y/.X (geography column)
            // can't be translated to SQL by the Npgsql provider.
            return await baseQuery
                .OrderByDescending(s => s.CreatedAt)
                .ToPagedResponseAsync(
                    query.Page,
                    query.PageSize,
                    s => new SpotSearchResultResponse(
                        s.Id,
                        s.Name,
                        s.Description,
                        s.Category,
                        s.PhotoUrls[0],
                        s.Location.Y,
                        s.Location.X,
                        s.CreatedByUserId,
                        s.CreatedAt),
                    cancellationToken);
        }
    }
}
