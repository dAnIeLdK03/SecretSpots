namespace SecretSpots.Features.Rewards;

public record RedemptionsPageResponse(
    IReadOnlyList<MyRedemptionResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
