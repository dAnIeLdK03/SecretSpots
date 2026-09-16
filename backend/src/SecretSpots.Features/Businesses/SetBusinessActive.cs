using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Businesses;

public static class SetBusinessActive
{
    // BusinessId comes from the route, not the request body — same convention used across
    // this feature's other owner-action endpoints.
    public record RequestBody(bool IsActive);

    public record Command(Guid BusinessId, bool IsActive) : IRequest<Result<BusinessResponse>>;

    public class Handler(IAppDbContext db, IUserContext userContext, IStringLocalizer<SharedResources> localizer, ILogger<Handler> logger)
        : IRequestHandler<Command, Result<BusinessResponse>>
    {
        public async Task<Result<BusinessResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            var business = await db.Businesses.SingleOrDefaultAsync(b => b.Id == command.BusinessId, cancellationToken);
            if (business is null)
            {
                return Result<BusinessResponse>.Failure(new Error(
                    BusinessesMessageKeys.NotFound,
                    localizer[BusinessesMessageKeys.NotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            if (business.OwnerUserId != userContext.UserId)
            {
                return Result<BusinessResponse>.Failure(new Error(
                    BusinessesMessageKeys.NotYourBusiness,
                    localizer[BusinessesMessageKeys.NotYourBusiness].Value,
                    StatusCodes.Status403Forbidden));
            }

            business.IsActive = command.IsActive;
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(BusinessesLogMessages.BusinessActiveStateChanged, business.Id, business.IsActive, userContext.UserId);

            return Result<BusinessResponse>.Success(new BusinessResponse(
                business.Id,
                business.Name,
                business.Description,
                business.Location.Y,
                business.Location.X,
                business.OwnerUserId,
                business.IsPromoted,
                business.IsActive,
                business.CreatedAt));
        }
    }
}
