namespace SecretSpots.Domain;

public class Rating : IHasCreatedAt
{
    public Guid Id { get; set; }
    public Guid SpotId { get; set; }
    public Guid UserId { get; set; }
    public int Value { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
