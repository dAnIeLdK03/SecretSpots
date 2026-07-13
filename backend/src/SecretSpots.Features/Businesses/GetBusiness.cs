using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;

namespace SecretSpots.Features.Businesses;

public static class GetBusiness
{
    public record Query(Guid BusinessId) : IRequest<Result<BusinessResponse>>;

    public class Handler(IAppDbContext db, IStringLocalizer<SharedResources> localizer)
        : IRequestHandler<Query, Result<BusinessResponse>>
    {
        public async Task<Result<BusinessResponse>> Handle(Query query, CancellationToken cancellationToken)
        {
            var business = await db.Businesses.SingleOrDefaultAsync(b => b.Id == query.BusinessId, cancellationToken);
            if (business is null)
            {
                return Result<BusinessResponse>.Failure(new Error(
                    BusinessesMessageKeys.NotFound,
                    localizer[BusinessesMessageKeys.NotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            return Result<BusinessResponse>.Success(new BusinessResponse(
                business.Id,
                business.Name,
                business.Description,
                business.Location.Y,
                business.Location.X,
                business.OwnerUserId,
                business.IsPromoted,
                business.CreatedAt));
        }
    }
}
