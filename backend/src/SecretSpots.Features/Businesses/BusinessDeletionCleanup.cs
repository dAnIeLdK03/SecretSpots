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

        // Rewards don't outlive their business — Reward.BusinessId has no DB-level FK (same as
        // every other Spot-adjacent table in this app), so nothing else would clean these up.
        //
        // RewardRedemptions are deliberately NOT deleted here: RewardTitle/BusinessName are
        // denormalized onto each one at redeem time exactly so a user's spend history survives
        // the reward and business being gone. BusinessId/RewardId above are left dangling on
        // those rows, same as every other FK-less reference in this app.
        await db.Rewards
            .Where(r => r.BusinessId == business.Id)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
