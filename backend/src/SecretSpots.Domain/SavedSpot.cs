namespace SecretSpots.Domain;

public class SavedSpot : IHasCreatedAt
{
    public Guid Id { get; set; }
    public Guid SpotId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
