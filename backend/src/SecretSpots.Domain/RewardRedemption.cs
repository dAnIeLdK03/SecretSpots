namespace SecretSpots.Domain;

public class RewardRedemption : IHasCreatedAt
{
    public Guid Id { get; set; }
    public Guid RewardId { get; set; }
    public Guid BusinessId { get; set; }
    public Guid UserId { get; set; }
    public int CrystalsSpent { get; set; }

    // Shown to the customer and matched visually by staff at the counter — not a bearer secret
    // (it doesn't grant the crystals; those are already spent) so it's stored as plain text, not
    // hashed like a login/reset token.
    public required string RedemptionCode { get; set; }

    public DateTimeOffset? FulfilledAt { get; set; }
    public Guid? FulfilledByUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
