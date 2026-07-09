using NetTopologySuite.Geometries;

namespace SecretSpots.Domain;

public class Spot : IHasCreatedAt
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public SpotCategory Category { get; set; }
    public required Point Location { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
