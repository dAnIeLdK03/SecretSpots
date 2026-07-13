namespace SecretSpots.Features.Rewards;

public record RewardRedemptionResponse(
    Guid RedemptionId,
    Guid RewardId,
    int CrystalsSpent,
    int NewCrystalBalance,
    DateTimeOffset CreatedAt);
