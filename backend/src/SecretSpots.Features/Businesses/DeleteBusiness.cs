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

public static class DeleteBusiness
{
    public record Command(Guid BusinessId) : IRequest<Result<Unit>>;

    public class Handler(IAppDbContext db, IUserContext userContext, IStringLocalizer<SharedResources> localizer, ILogger<Handler> logger)
        : IRequestHandler<Command, Result<Unit>>
    {
        public async Task<Result<Unit>> Handle(Command command, CancellationToken cancellationToken)
        {
            var business = await db.Businesses.SingleOrDefaultAsync(b => b.Id == command.BusinessId, cancellationToken);
            if (business is null)
            {
                return Result<Unit>.Failure(new Error(
                    BusinessesMessageKeys.NotFound,
                    localizer[BusinessesMessageKeys.NotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            if (business.OwnerUserId != userContext.UserId)
            {
                return Result<Unit>.Failure(new Error(
                    BusinessesMessageKeys.NotYourBusiness,
                    localizer[BusinessesMessageKeys.NotYourBusiness].Value,
                    StatusCodes.Status403Forbidden));
            }

            await BusinessDeletionCleanup.DeleteAsync(db, business, cancellationToken);

            logger.LogInformation(BusinessesLogMessages.BusinessDeleted, business.Id, userContext.UserId);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
