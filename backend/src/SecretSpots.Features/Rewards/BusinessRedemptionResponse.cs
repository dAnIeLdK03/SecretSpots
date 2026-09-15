namespace SecretSpots.Features.Rewards;

public record BusinessRedemptionResponse(
    Guid RedemptionId,
    Guid RewardId,
    string RewardTitle,
    string RedeemedByDisplayName,
    int CrystalsSpent,
    string RedemptionCode,
    bool IsFulfilled,
    DateTimeOffset? FulfilledAt,
    DateTimeOffset CreatedAt);
