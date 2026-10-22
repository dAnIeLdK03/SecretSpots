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
}
