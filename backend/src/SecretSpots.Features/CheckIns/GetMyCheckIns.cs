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

namespace SecretSpots.Features.CheckIns;

public static class GetMyCheckIns
{
    public record Query(int Page, int PageSize) : IRequest<PagedResponse<MyCheckInResponse>>;

    public class Validator : AbstractValidator<Query>
    {
        public Validator(IStringLocalizer<SharedResources> localizer, IOptions<CheckInOptions> checkInOptions)
        {
            this.ValidatePagination(
                q => q.Page,
                q => q.PageSize,
                checkInOptions.Value.MaxPageSize,
                localizer[CheckInsMessageKeys.PageOutOfRange].Value,
                localizer[CheckInsMessageKeys.PageSizeOutOfRange].Value);
        }
    }

    public class Handler(IAppDbContext db, IUserContext userContext) : IRequestHandler<Query, PagedResponse<MyCheckInResponse>>
    {
        public async Task<PagedResponse<MyCheckInResponse>> Handle(Query query, CancellationToken cancellationToken)
        {
            var baseQuery =
                from checkIn in db.CheckIns
                join spot in db.Spots on checkIn.SpotId equals spot.Id
                where checkIn.UserId == userContext.UserId
                select new { checkIn, spot.Name };

            return await baseQuery
                .OrderByDescending(x => x.checkIn.CreatedAt)
                .Select(x => new MyCheckInResponse(
                    x.checkIn.Id, x.checkIn.SpotId, x.Name, x.checkIn.PhotoUrl, x.checkIn.CrystalsAwarded, x.checkIn.CreatedAt))
                .ToPagedResponseAsync(query.Page, query.PageSize, cancellationToken);
        }
    }
}
