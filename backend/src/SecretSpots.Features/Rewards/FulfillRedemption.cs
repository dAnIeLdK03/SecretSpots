using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Rewards;

public static class FulfillRedemption
{
    public record Command(Guid RedemptionId) : IRequest<Result<Unit>>;

    public class Handler(IAppDbContext db, IUserContext userContext, IStringLocalizer<SharedResources> localizer, ILogger<Handler> logger)
        : IRequestHandler<Command, Result<Unit>>
    {
        public async Task<Result<Unit>> Handle(Command command, CancellationToken cancellationToken)
        {
            var redemption = await db.RewardRedemptions
                .SingleOrDefaultAsync(r => r.Id == command.RedemptionId, cancellationToken);
            if (redemption is null)
            {
                return Result<Unit>.Failure(new Error(
                    RewardsMessageKeys.RedemptionNotFound,
                    localizer[RewardsMessageKeys.RedemptionNotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            var business = await db.Businesses.SingleAsync(b => b.Id == redemption.BusinessId, cancellationToken);
            if (business.OwnerUserId != userContext.UserId)
            {
                return Result<Unit>.Failure(new Error(
                    RewardsMessageKeys.NotYourBusiness,
                    localizer[RewardsMessageKeys.NotYourBusiness].Value,
                    StatusCodes.Status403Forbidden));
            }

            if (redemption.FulfilledAt is not null)
            {
                return Result<Unit>.Failure(new Error(
                    RewardsMessageKeys.AlreadyFulfilled,
                    localizer[RewardsMessageKeys.AlreadyFulfilled].Value,
                    StatusCodes.Status409Conflict));
            }

            redemption.FulfilledAt = DateTimeOffset.UtcNow;
            redemption.FulfilledByUserId = userContext.UserId;
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(RewardsLogMessages.RedemptionFulfilled, redemption.Id, userContext.UserId);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
