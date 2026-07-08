namespace SecretSpots.Domain;

public class CheckIn
{
    public Guid Id { get; set; }
    public Guid SpotId { get; set; }
    public Guid UserId { get; set; }
    public required string PhotoUrl { get; set; }
    public int CrystalsAwarded { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
