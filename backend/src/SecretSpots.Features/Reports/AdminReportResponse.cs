using SecretSpots.Domain;

namespace SecretSpots.Features.Reports;

public record AdminReportResponse(
    Guid Id,
    ReportedContentType ContentType,
    Guid ContentId,
    Guid? RelatedSpotId,
    string? ContentPreview,
    string ReporterDisplayName,
    ReportReason Reason,
    string? Details,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt);
