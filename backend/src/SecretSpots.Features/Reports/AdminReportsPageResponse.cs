namespace SecretSpots.Features.Reports;

public record AdminReportsPageResponse(
    IReadOnlyList<AdminReportResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
