namespace SecretSpots.Features.Businesses;

public record NearbyBusinessResponse(
    Guid Id,
    string Name,
    string Description,
    double Latitude,
    double Longitude,
    bool IsPromoted,
    double DistanceKm);
