namespace SecretSpots.Domain;

public class Notification : IHasCreatedAt
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public Guid? RelatedSpotId { get; set; }
    public int? CrystalsAwarded { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
