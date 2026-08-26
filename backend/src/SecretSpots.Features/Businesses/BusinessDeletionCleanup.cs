using Microsoft.EntityFrameworkCore;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;

namespace SecretSpots.Features.Businesses;

// Shared by DeleteBusiness and DeleteAccount (deleting an account deletes every business it
// owns) — keeps the cascade logic in one place so the two can't drift apart.
public static class BusinessDeletionCleanup
{
    public static async Task DeleteAsync(IAppDbContext db, Business business, CancellationToken cancellationToken)
    {
        db.Businesses.Remove(business);
        await db.SaveChangesAsync(cancellationToken);

        // Neither Reward.BusinessId nor RewardRedemption.BusinessId has a DB-level FK (same as
        // every other Spot-adjacent table in this app), so nothing stops them existing without a
        // business — clean them up the same way DeleteSpot/DeleteReward do.
        await db.RewardRedemptions
            .Where(r => r.BusinessId == business.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await db.Rewards
            .Where(r => r.BusinessId == business.Id)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
