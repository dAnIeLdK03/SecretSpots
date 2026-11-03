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

    // Applies this pending edit to its spot (the proposal is the full new state, not a diff) and marks
    // it approved. Returns the photos the edit dropped from the spot — the caller deletes them from
    // storage. An approved deletion has nothing to apply: the spot and this row are deleted instead.
    public IReadOnlyList<string> ApplyTo(Spot spot, Guid reviewerId, DateTimeOffset now)
    {
        EnsurePending();
        if (Type != SpotChangeRequestType.Edit)
        {
            throw new InvalidOperationException($"Only an edit can be applied; this request is a {Type}.");
        }

        if (spot.Id != SpotId)
        {
            throw new InvalidOperationException($"Change request {Id} is for spot {SpotId}, not {spot.Id}.");
        }

        var removedPhotoUrls = spot.PhotoUrls.Except(ProposedPhotoUrls!).ToList();
        spot.Name = ProposedName!;
        spot.Description = ProposedDescription!;
        spot.Category = ProposedCategory!.Value;
        spot.PhotoUrls = ProposedPhotoUrls!.ToList();

        MarkReviewed(SpotChangeRequestStatus.Approved, reviewerId, now);
        return removedPhotoUrls;
    }

    // Rejects this pending request; the spot stays as it is. Returns what to tell the requester.
    public NotificationType Reject(string reason, Guid reviewerId, DateTimeOffset now)
    {
        EnsurePending();
        RejectionReason = reason.Trim();
        MarkReviewed(SpotChangeRequestStatus.Rejected, reviewerId, now);
        return Type == SpotChangeRequestType.Deletion
            ? NotificationType.SpotDeletionRejected
            : NotificationType.SpotEditRejected;
    }

    private void EnsurePending()
    {
        if (Status != SpotChangeRequestStatus.Pending)
        {
            throw new InvalidOperationException($"Change request {Id} was already reviewed ({Status}).");
        }
    }

    private void MarkReviewed(SpotChangeRequestStatus status, Guid reviewerId, DateTimeOffset now)
    {
        Status = status;
        ReviewedByUserId = reviewerId;
        ReviewedAt = now;
    }
}
