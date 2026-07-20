namespace SecretSpots.Features.CheckIns;

public record CheckInsPageResponse(
    IReadOnlyList<MyCheckInResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
