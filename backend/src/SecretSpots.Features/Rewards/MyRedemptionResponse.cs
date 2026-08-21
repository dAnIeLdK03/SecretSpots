namespace SecretSpots.Features.Rewards;

public record MyRedemptionResponse(
    Guid RedemptionId,
    Guid RewardId,
    string RewardTitle,
    Guid BusinessId,
    string BusinessName,
    int CrystalsSpent,
    DateTimeOffset CreatedAt);
