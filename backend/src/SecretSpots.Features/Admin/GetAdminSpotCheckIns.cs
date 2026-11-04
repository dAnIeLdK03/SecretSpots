using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Validation;

namespace SecretSpots.Features.Admin;

// Check-ins at one spot, newest first — the photos an admin can add to the spot's own gallery.
public static class GetAdminSpotCheckIns
{
    public record Query(Guid SpotId, int Page, int PageSize) : IRequest<Result<PagedResponse<AdminSpotCheckInResponse>>>;

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

    public class Handler(IAppDbContext db, IStringLocalizer<SharedResources> localizer)
        : IRequestHandler<Query, Result<PagedResponse<AdminSpotCheckInResponse>>>
    {
        public async Task<Result<PagedResponse<AdminSpotCheckInResponse>>> Handle(Query query, CancellationToken cancellationToken)
        {
            var spotPhotoUrls = await db.Spots
                .Where(s => s.Id == query.SpotId)
                .Select(s => s.PhotoUrls)
                .SingleOrDefaultAsync(cancellationToken);
            if (spotPhotoUrls is null)
            {
                return Result<PagedResponse<AdminSpotCheckInResponse>>.Failure(Error.NotFound(AdminMessageKeys.SpotNotFound, localizer));
            }

            var checkIns =
                from checkIn in db.CheckIns
                join user in db.Users on checkIn.UserId equals user.Id
                where checkIn.SpotId == query.SpotId
                select new { checkIn, user.DisplayName };

            var page = await checkIns
                .OrderByDescending(x => x.checkIn.CreatedAt)
                .ToPagedResponseAsync(
                    query.Page,
                    query.PageSize,
                    x => new AdminSpotCheckInResponse(
                        x.checkIn.Id,
                        x.checkIn.UserId,
                        x.DisplayName,
                        x.checkIn.PhotoUrl,
                        x.checkIn.CreatedAt,
                        spotPhotoUrls.Contains(x.checkIn.PhotoUrl)),
                    cancellationToken);

            return Result<PagedResponse<AdminSpotCheckInResponse>>.Success(page);
        }
    }
}
