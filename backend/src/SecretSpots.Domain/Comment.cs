namespace SecretSpots.Domain;

public class Comment : IHasCreatedAt
{
    public Guid Id { get; set; }
    public Guid SpotId { get; set; }
    public Guid UserId { get; set; }
    public required string Text { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
