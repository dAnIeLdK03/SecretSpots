namespace SecretSpots.Features.Rewards;

public record RewardsPageResponse(
    IReadOnlyList<RewardResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
