namespace SecretSpots.Domain;

public class ExternalLogin : IHasCreatedAt
{
    public Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required ExternalAuthProvider Provider { get; set; }
    public required string ProviderUserId { get; set; }
    public required string Email { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}