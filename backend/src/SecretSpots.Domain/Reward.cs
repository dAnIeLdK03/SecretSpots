namespace SecretSpots.Domain;

public class Reward : IHasCreatedAt
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public int CrystalCost { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
