namespace SecretSpots.Domain;

// A spot's creator can't edit or delete it directly — every change is proposed here and only
// applied once an admin approves it, so curated content (photos, wording) can't be silently
// undone by the creator.
public class SpotChangeRequest : IHasCreatedAt
{
    public Guid Id { get; set; }
    public Guid SpotId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public SpotChangeRequestType Type { get; set; }
    public SpotChangeRequestStatus Status { get; set; } = SpotChangeRequestStatus.Pending;

    // Only set for Type == Edit — the full proposed state, not a diff.
    public string? ProposedName { get; set; }
    public string? ProposedDescription { get; set; }
    public SpotCategory? ProposedCategory { get; set; }
    public List<string>? ProposedPhotoUrls { get; set; }

    public string? RejectionReason { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
