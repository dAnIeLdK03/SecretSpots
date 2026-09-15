namespace SecretSpots.Features.Rewards;

public record BusinessRedemptionsPageResponse(
    IReadOnlyList<BusinessRedemptionResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
