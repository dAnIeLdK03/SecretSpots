using SecretSpots.Domain;

namespace SecretSpots.Features.Spots;

public record NearbySpotResponse(
    Guid Id,
    string Name,
    string Description,
    SpotCategory Category,
    string PhotoUrl,
    double Latitude,
    double Longitude,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    double DistanceKm);
