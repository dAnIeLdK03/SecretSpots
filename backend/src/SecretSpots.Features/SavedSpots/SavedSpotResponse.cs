using SecretSpots.Domain;

namespace SecretSpots.Features.SavedSpots;

public record SavedSpotResponse(
    Guid SpotId,
    string SpotName,
    string PhotoUrl,
    SpotCategory Category,
    DateTimeOffset SavedAt);
