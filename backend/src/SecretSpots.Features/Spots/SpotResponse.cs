using SecretSpots.Domain;

namespace SecretSpots.Features.Spots;

public record SpotResponse(
    Guid Id,
    string Name,
    string Description,
    SpotCategory Category,
    string PhotoUrl,
    double Latitude,
    double Longitude,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt);
