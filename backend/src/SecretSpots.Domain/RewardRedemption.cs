namespace SecretSpots.Domain;

public class RewardRedemption : IHasCreatedAt
{
    public Guid Id { get; set; }
    public Guid RewardId { get; set; }
    public Guid BusinessId { get; set; }
    public Guid UserId { get; set; }
    public int CrystalsSpent { get; set; }

    // Denormalized as of redemption time, not looked up live from Reward/Business — those rows
    // (and the crystals-for-them cost) can be deleted later (DeleteReward, BusinessDeletionCleanup),
    // but a user's redemption history has to keep reading correctly regardless. RewardId/BusinessId
    // above are then left pointing at rows that may no longer exist, same as every other
    // FK-less reference in this app.
    public required string RewardTitle { get; set; }
    public required string BusinessName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
