namespace SecretSpots.Domain;

public class Report : IHasCreatedAt
{
    public Guid Id { get; set; }
    public ReportedContentType ContentType { get; set; }
    public Guid ContentId { get; set; }
    public Guid ReporterUserId { get; set; }
    public ReportReason Reason { get; set; }
    public string? Details { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
}
