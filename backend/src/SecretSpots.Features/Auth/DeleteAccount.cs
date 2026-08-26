using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecretSpots.Features.Businesses;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Common.Storage;
using SecretSpots.Features.Spots;

namespace SecretSpots.Features.Auth;

public static class DeleteAccount
{
    public record Command : IRequest<Result<Unit>>;

    public class Handler(
        IAppDbContext db,
        IUserContext userContext,
        IPhotoStorage photoStorage,
        IStringLocalizer<SharedResources> localizer,
        ILogger<Handler> logger)
        : IRequestHandler<Command, Result<Unit>>
    {
        public async Task<Result<Unit>> Handle(Command command, CancellationToken cancellationToken)
        {
            var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);
            if (user is null)
            {
                return Result<Unit>.Failure(new Error(
                    AuthMessageKeys.UserNotFound,
                    localizer[AuthMessageKeys.UserNotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            await using var transaction = await db.BeginTransactionAsync(cancellationToken);

            // Owned spots/businesses first — each already has its own cascading cleanup
            // (comments, ratings, saved spots, check-ins, R2 photos, reward redemptions/rewards).
            var ownedSpots = await db.Spots.Where(s => s.CreatedByUserId == user.Id).ToListAsync(cancellationToken);
            foreach (var spot in ownedSpots)
            {
                await SpotDeletionCleanup.DeleteAsync(db, photoStorage, spot, logger, cancellationToken);
            }

            var ownedBusinesses = await db.Businesses.Where(b => b.OwnerUserId == user.Id).ToListAsync(cancellationToken);
            foreach (var business in ownedBusinesses)
            {
                await BusinessDeletionCleanup.DeleteAsync(db, business, cancellationToken);
            }

            // Ratings this user left on spots someone else owns — deleting them changes those
            // spots' denormalized AverageRating/RatingsCount (see RateSpot.Handler), so the
            // affected spot ids need to be gathered before the rows are gone.
            var ratedSpotIds = await db.Ratings
                .Where(r => r.UserId == user.Id)
                .Select(r => r.SpotId)
                .Distinct()
                .ToListAsync(cancellationToken);

            await db.Ratings.Where(r => r.UserId == user.Id).ExecuteDeleteAsync(cancellationToken);

            foreach (var spotId in ratedSpotIds)
            {
                await RecomputeRatingStatsAsync(db, spotId, cancellationToken);
            }

            // Comments/saved spots/check-ins/redemptions/notifications left on content that's
            // still owned by someone else (anything on this user's own spots/businesses is
            // already gone from the cascades above).
            await db.Comments.Where(c => c.UserId == user.Id).ExecuteDeleteAsync(cancellationToken);
            await db.SavedSpots.Where(s => s.UserId == user.Id).ExecuteDeleteAsync(cancellationToken);
            await db.CheckIns.Where(c => c.UserId == user.Id).ExecuteDeleteAsync(cancellationToken);
            await db.RewardRedemptions.Where(r => r.UserId == user.Id).ExecuteDeleteAsync(cancellationToken);
            await db.Notifications.Where(n => n.UserId == user.Id).ExecuteDeleteAsync(cancellationToken);

            // Auth-related rows tied to this account.
            await db.RefreshTokens.Where(t => t.UserId == user.Id).ExecuteDeleteAsync(cancellationToken);
            await db.PasswordResetTokens.Where(t => t.UserId == user.Id).ExecuteDeleteAsync(cancellationToken);
            await db.ExternalLogins.Where(l => l.UserId == user.Id).ExecuteDeleteAsync(cancellationToken);
            await db.ExternalAuthTransactions.Where(t => t.UserId == user.Id).ExecuteDeleteAsync(cancellationToken);

            db.Users.Remove(user);
            await db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(AuthLogMessages.AccountDeleted, user.Id);

            return Result<Unit>.Success(Unit.Value);
        }

        // A user's last rating on a spot leaves zero rows behind, which GroupBy produces no
        // groups for — unlike RateSpot.Handler's inline version of this query (which only ever
        // adds/updates a rating, so the "zero remain" case can't occur there), this must handle
        // it explicitly rather than via SingleAsync.
        private static async Task RecomputeRatingStatsAsync(IAppDbContext db, Guid spotId, CancellationToken cancellationToken)
        {
            var spot = await db.Spots.SingleOrDefaultAsync(s => s.Id == spotId, cancellationToken);
            if (spot is null)
            {
                return;
            }

            var stats = await db.Ratings
                .Where(r => r.SpotId == spotId)
                .GroupBy(r => r.SpotId)
                .Select(g => new { Average = g.Average(r => r.Value), Count = g.Count() })
                .SingleOrDefaultAsync(cancellationToken);

            spot.AverageRating = stats?.Average ?? 0;
            spot.RatingsCount = stats?.Count ?? 0;
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
