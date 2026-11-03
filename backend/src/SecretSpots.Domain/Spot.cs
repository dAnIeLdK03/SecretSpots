using NetTopologySuite.Geometries;

namespace SecretSpots.Domain;

public class Spot : IHasCreatedAt
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public SpotCategory Category { get; set; }
    public required Point Location { get; set; }
    public required List<string> PhotoUrls { get; set; }
    public Guid CreatedByUserId { get; set; }
    public double AverageRating { get; set; }
    public int RatingsCount { get; set; }
    public SpotModerationStatus ModerationStatus { get; set; } = SpotModerationStatus.Pending;
    public string? RejectionReason { get; set; }

    // Set when the spot is rejected (now + the grace period) and cleared if it's approved again.
    // Once it passes, RejectedSpotCleanupService soft-deletes the spot by setting DeletedAt.
    public DateTimeOffset? ScheduledDeletionAt { get; set; }

    // Soft delete — the row and everything attached to it stay in the DB, but the global query
    // filter in AppDbContext hides it from every query.
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Approves the spot, or restores it if it was rejected. Returns what to tell its creator, or null
    // when it was already approved (nothing changed for them). A rejection reason or pending
    // deletion never lingers on an approved spot.
    public NotificationType? Approve()
    {
        NotificationType? notification = ModerationStatus switch
        {
            SpotModerationStatus.Approved => null,
            SpotModerationStatus.Rejected => NotificationType.SpotRestored,
            _ => NotificationType.SpotApproved,
        };

        ModerationStatus = SpotModerationStatus.Approved;
        RejectionReason = null;
        ScheduledDeletionAt = null;
        return notification;
    }

    // Rejects the spot, or takes it down if it was approved, and schedules its soft delete after the
    // grace period. Returns what to tell its creator, or null when it was already rejected — then
    // only the reason's wording changes and the original deadline stands (no extension by re-rejecting).
    public NotificationType? Reject(string reason, DateTimeOffset now, TimeSpan gracePeriod)
    {
        NotificationType? notification = ModerationStatus switch
        {
            SpotModerationStatus.Rejected => null,
            SpotModerationStatus.Approved => NotificationType.SpotRemoved,
            _ => NotificationType.SpotRejected,
        };

        ModerationStatus = SpotModerationStatus.Rejected;
        RejectionReason = reason.Trim();
        ScheduledDeletionAt ??= now + gracePeriod;
        return notification;
    }
}
