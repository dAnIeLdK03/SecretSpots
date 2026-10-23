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

// Pending requests only, oldest first — it's a review queue.
public static class GetAdminSpotChangeRequests
{
    public record Query(int Page, int PageSize) : IRequest<PagedResponse<AdminSpotChangeRequestResponse>>;

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

    public class Handler(IAppDbContext db) : IRequestHandler<Query, PagedResponse<AdminSpotChangeRequestResponse>>
    {
        public async Task<PagedResponse<AdminSpotChangeRequestResponse>> Handle(Query query, CancellationToken cancellationToken)
        {
            var pending =
                from request in db.SpotChangeRequests
                join spot in db.Spots on request.SpotId equals spot.Id
                join user in db.Users on request.RequestedByUserId equals user.Id
                where request.Status == SpotChangeRequestStatus.Pending
                select new { request, spot.Name, spot.Description, spot.Category, spot.PhotoUrls, user.DisplayName };

            return await pending
                .OrderBy(x => x.request.CreatedAt)
                .Select(x => new AdminSpotChangeRequestResponse(
                    x.request.Id,
                    x.request.Type,
                    x.request.CreatedAt,
                    x.request.RequestedByUserId,
                    x.DisplayName,
                    x.request.SpotId,
                    x.Name,
                    x.Description,
                    x.Category,
                    x.PhotoUrls,
                    x.request.ProposedName,
                    x.request.ProposedDescription,
                    x.request.ProposedCategory,
                    x.request.ProposedPhotoUrls))
                .ToPagedResponseAsync(query.Page, query.PageSize, cancellationToken);
        }
    }
}
