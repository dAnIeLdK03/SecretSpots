namespace SecretSpots.Domain;

public class RoleChangeAudit : IHasCreatedAt
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public UserRole OldRole { get; set; }
    public UserRole NewRole { get; set; }
    public Guid ChangedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
