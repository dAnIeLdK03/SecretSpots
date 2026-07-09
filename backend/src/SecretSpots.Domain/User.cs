namespace SecretSpots.Domain;

public class User : IHasCreatedAt
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string DisplayName { get; set; }
    public int CrystalBalance { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
