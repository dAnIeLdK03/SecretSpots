using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.CheckIns;

public static class GetMyCheckIns
{
    public record Query(int Page, int PageSize) : IRequest<CheckInsPageResponse>;

    public class Validator : AbstractValidator<Query>
    {
        public Validator(IStringLocalizer<SharedResources> localizer, IOptions<CheckInOptions> checkInOptions)
        {
            RuleFor(q => q.Page)
                .GreaterThanOrEqualTo(1).WithMessage(localizer[CheckInsMessageKeys.PageOutOfRange].Value);

            RuleFor(q => q.PageSize)
                .InclusiveBetween(1, checkInOptions.Value.MaxPageSize)
                    .WithMessage(localizer[CheckInsMessageKeys.PageSizeOutOfRange].Value);
        }
    }

    public class Handler(IAppDbContext db, IUserContext userContext) : IRequestHandler<Query, CheckInsPageResponse>
    {
        public async Task<CheckInsPageResponse> Handle(Query query, CancellationToken cancellationToken)
        {
            var baseQuery =
                from checkIn in db.CheckIns
                join spot in db.Spots on checkIn.SpotId equals spot.Id
                where checkIn.UserId == userContext.UserId
                select new { checkIn, spot.Name };

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var items = await baseQuery
                .OrderByDescending(x => x.checkIn.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(x => new MyCheckInResponse(
                    x.checkIn.Id, x.checkIn.SpotId, x.Name, x.checkIn.PhotoUrl, x.checkIn.CrystalsAwarded, x.checkIn.CreatedAt))
                .ToListAsync(cancellationToken);

            return new CheckInsPageResponse(items, query.Page, query.PageSize, totalCount);
        }
    }
}
