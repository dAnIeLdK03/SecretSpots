using FluentValidation;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Validation;
using SecretSpots.Features.Spots;

namespace SecretSpots.Features.Admin;

public static class GetAdminSpots
{
    public record Query(SpotModerationStatus? Status, int Page, int PageSize) : IRequest<PagedResponse<SpotSearchResultResponse>>;

    public class Validator : AbstractValidator<Query>
    {
        public Validator(IStringLocalizer<SharedResources> localizer, IOptions<AdminSpotsOptions> adminSpotsOptions)
        {
            this.ValidatePagination(
                q => q.Page,
                q => q.PageSize,
                adminSpotsOptions.Value.MaxPageSize,
                localizer[AdminMessageKeys.PageOutOfRange].Value,
                localizer[AdminMessageKeys.PageSizeOutOfRange].Value);
        }
    }

    public class Handler(IAppDbContext db) : IRequestHandler<Query, PagedResponse<SpotSearchResultResponse>>
    {
        public async Task<PagedResponse<SpotSearchResultResponse>> Handle(Query query, CancellationToken cancellationToken)
        {
            var baseQuery = db.Spots.AsQueryable();

            if (query.Status is not null)
            {
                baseQuery = baseQuery.Where(s => s.ModerationStatus == query.Status);
            }

            // Materialized first, then shaped in-memory — Location.Y/.X (geography column)
            // can't be translated to SQL by the Npgsql provider (same reasoning as GetMySpots).
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
                        s.CreatedAt,
                        s.ModerationStatus,
                        s.RejectionReason),
                    cancellationToken);
        }
    }
}
