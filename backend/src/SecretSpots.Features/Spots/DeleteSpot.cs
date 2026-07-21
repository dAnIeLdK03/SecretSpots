using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Spots;

public static class DeleteSpot
{
    public record Command(Guid SpotId) : IRequest<Result<Unit>>;

    public class Handler(IAppDbContext db, IUserContext userContext, IStringLocalizer<SharedResources> localizer, ILogger<Handler> logger)
        : IRequestHandler<Command, Result<Unit>>
    {
        public async Task<Result<Unit>> Handle(Command command, CancellationToken cancellationToken)
        {
            var spot = await db.Spots.SingleOrDefaultAsync(s => s.Id == command.SpotId, cancellationToken);
            if (spot is null)
            {
                return Result<Unit>.Failure(new Error(
                    SpotsMessageKeys.NotFound,
                    localizer[SpotsMessageKeys.NotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            if (spot.CreatedByUserId != userContext.UserId)
            {
                return Result<Unit>.Failure(new Error(
                    SpotsMessageKeys.NotYourSpot,
                    localizer[SpotsMessageKeys.NotYourSpot].Value,
                    StatusCodes.Status403Forbidden));
            }

            db.Spots.Remove(spot);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(SpotsLogMessages.SpotDeleted, spot.Id, userContext.UserId);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
