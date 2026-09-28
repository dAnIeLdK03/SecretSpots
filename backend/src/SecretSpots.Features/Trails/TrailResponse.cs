namespace SecretSpots.Features.Trails;

public record TrailPoint(double Latitude, double Longitude);

public record TrailResponse(
    Guid Id,
    string Name,
    string Description,
    IReadOnlyList<string> PhotoUrls,
    IReadOnlyList<TrailPoint> Points,
    double DistanceMeters,
    Guid CreatedByUserId,
    string CreatedByDisplayName,
    DateTimeOffset CreatedAt);
