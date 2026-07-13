namespace SecretSpots.Features.Rewards;

public record RewardResponse(
    Guid Id,
    Guid BusinessId,
    string Title,
    string Description,
    int CrystalCost,
    DateTimeOffset CreatedAt);
