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

namespace SecretSpots.Features.Admin;

public static class GetAdminSpots
{
    // CreatedByUserId narrows the list to one user's spots (the admin user details page).
    public record Query(SpotModerationStatus? Status, Guid? CreatedByUserId, int Page, int PageSize)
        : IRequest<PagedResponse<AdminSpotResponse>>;

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

    public class Handler(IAppDbContext db) : IRequestHandler<Query, PagedResponse<AdminSpotResponse>>
    {
        public async Task<PagedResponse<AdminSpotResponse>> Handle(Query query, CancellationToken cancellationToken)
        {
            var baseQuery = db.Spots.AsQueryable();

            if (query.Status is not null)
            {
                baseQuery = baseQuery.Where(s => s.ModerationStatus == query.Status);
            }

            if (query.CreatedByUserId is not null)
            {
                baseQuery = baseQuery.Where(s => s.CreatedByUserId == query.CreatedByUserId);
            }

            var withCreator =
                from spot in baseQuery
                join user in db.Users on spot.CreatedByUserId equals user.Id
                select new { spot, user.DisplayName };

            // Materialized first, then shaped in-memory — Location.Y/.X (geography column)
            // can't be translated to SQL by the Npgsql provider (same reasoning as GetMySpots).
            return await withCreator
                .OrderByDescending(x => x.spot.CreatedAt)
                .ToPagedResponseAsync(
                    query.Page,
                    query.PageSize,
                    x => new AdminSpotResponse(
                        x.spot.Id,
                        x.spot.Name,
                        x.spot.Description,
                        x.spot.Category,
                        x.spot.PhotoUrls[0],
                        x.spot.Location.Y,
                        x.spot.Location.X,
                        x.spot.CreatedByUserId,
                        x.DisplayName,
                        x.spot.CreatedAt,
                        x.spot.ModerationStatus,
                        x.spot.RejectionReason,
                        x.spot.ScheduledDeletionAt),
                    cancellationToken);
        }
    }
}
