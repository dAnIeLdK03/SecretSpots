namespace SecretSpots.Domain;

public class Reward : IHasCreatedAt
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public int CrystalCost { get; set; }

    // Lets a business owner pause a reward (e.g. out of stock) without losing its redemption
    // history — the alternative, deleting it, is exactly the data-loss problem
    // RewardRedemption.RewardTitle/BusinessName denormalization was added to avoid.
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
}
