namespace SecretSpots.Domain;

public class RewardRedemption : IHasCreatedAt
{
    public Guid Id { get; set; }
    public Guid RewardId { get; set; }
    public Guid BusinessId { get; set; }
    public Guid UserId { get; set; }
    public int CrystalsSpent { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
