namespace SecretSpots.Domain;

// One row per balance change — lets a wallet's Balance always be reconstructed/audited from its
// transactions, even though the Handlers still maintain Balance directly rather than deriving it
// from this table on every read (same denormalized-cache-plus-log shape as Spot.AverageRating).
public class CrystalTransaction : IHasCreatedAt
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    // Positive for an earn, negative for a spend.
    public int Amount { get; set; }
    public CrystalTransactionReason Reason { get; set; }

    // Only one of these is ever set, matching whichever Reason this row has — same nullable-
    // optional-reference pattern as Notification.RelatedSpotId.
    public Guid? RelatedCheckInId { get; set; }
    public Guid? RelatedRedemptionId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
