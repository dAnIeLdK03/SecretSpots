using NetTopologySuite.Geometries;

namespace SecretSpots.Domain;

public class Trail : IHasCreatedAt
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required LineString Path { get; set; }
    public double DistanceMeters { get; set; }
    public required List<string> PhotoUrls { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
