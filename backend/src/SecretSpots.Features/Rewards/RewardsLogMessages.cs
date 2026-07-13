namespace SecretSpots.Features.Rewards;

internal static class RewardsLogMessages
{
    public const string RewardCreated = "Reward {RewardId} created for business {BusinessId} by user {UserId}.";
    public const string RewardUpdated = "Reward {RewardId} updated by user {UserId}.";
    public const string RewardDeleted = "Reward {RewardId} deleted by user {UserId}.";
    public const string RewardRedeemed = "Reward {RewardId} redeemed by user {UserId} for {CrystalsSpent} crystals.";
}
