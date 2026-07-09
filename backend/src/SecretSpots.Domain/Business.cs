using NetTopologySuite.Geometries;

namespace SecretSpots.Domain;

public class Business : IHasCreatedAt
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required Point Location { get; set; }
    public Guid OwnerUserId { get; set; }
    public bool IsPromoted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
