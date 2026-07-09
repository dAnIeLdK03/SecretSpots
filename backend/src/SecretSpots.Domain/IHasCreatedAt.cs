namespace SecretSpots.Domain;

// Implemented by entities that track when they were created. AppDbContext stamps
// CreatedAt itself on insert, so callers never need to (and can't forget to) set it.
public interface IHasCreatedAt
{
    DateTimeOffset CreatedAt { get; set; }
}
