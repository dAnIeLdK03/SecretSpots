namespace SecretSpots.Features.SavedSpots;

public record SavedSpotsPageResponse(
    IReadOnlyList<SavedSpotResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
