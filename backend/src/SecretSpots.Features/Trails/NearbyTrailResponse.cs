namespace SecretSpots.Features.Trails;

public record NearbyTrailResponse(
    Guid Id,
    string Name,
    string Description,
    string PhotoUrl,
    IReadOnlyList<TrailPoint> Points,
    double DistanceMeters,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt);

public record NearbyTrailsResponse(IReadOnlyList<NearbyTrailResponse> Items, int TotalCount);
