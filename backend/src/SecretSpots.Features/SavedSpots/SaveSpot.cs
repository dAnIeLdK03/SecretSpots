using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.SavedSpots;

public static class SaveSpot
{
    public record Command(Guid SpotId) : IRequest<Result<Unit>>;

    public class Handler(IAppDbContext db, IUserContext userContext, IStringLocalizer<SharedResources> localizer)
        : IRequestHandler<Command, Result<Unit>>
    {
        public async Task<Result<Unit>> Handle(Command command, CancellationToken cancellationToken)
        {
            var spotExists = await db.Spots.AnyAsync(s => s.Id == command.SpotId, cancellationToken);
            if (!spotExists)
            {
                return Result<Unit>.Failure(new Error(
                    SavedSpotsMessageKeys.SpotNotFound,
                    localizer[SavedSpotsMessageKeys.SpotNotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            var alreadySaved = await db.SavedSpots.AnyAsync(
                s => s.SpotId == command.SpotId && s.UserId == userContext.UserId, cancellationToken);

            if (!alreadySaved)
            {
                db.SavedSpots.Add(new SavedSpot
                {
                    Id = Guid.NewGuid(),
                    SpotId = command.SpotId,
                    UserId = userContext.UserId,
                });

                try
                {
                    await db.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException)
                {
                    // Another concurrent request (double-click, two tabs) already saved this
                    // spot for this user between the check above and this write — the unique
                    // (SpotId, UserId) index caught it. The end state is identical either way,
                    // so this is a success, not an error.
                }
            }

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
