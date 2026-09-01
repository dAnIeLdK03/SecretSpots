namespace SecretSpots.Features.Spots;

public record SpotSearchPageResponse(
    IReadOnlyList<SpotSearchResultResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
