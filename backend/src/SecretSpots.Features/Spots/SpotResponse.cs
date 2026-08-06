using SecretSpots.Domain;

namespace SecretSpots.Features.Spots;

public record SpotResponse(
    Guid Id,
    string Name,
    string Description,
    SpotCategory Category,
    IReadOnlyList<string> PhotoUrls,
    double Latitude,
    double Longitude,
    Guid CreatedByUserId,
    string CreatedByDisplayName,
    double AverageRating,
    int RatingsCount,
    DateTimeOffset CreatedAt);
