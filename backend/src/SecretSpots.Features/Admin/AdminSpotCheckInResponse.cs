namespace SecretSpots.Features.Admin;

// One visit's proof photo, as an admin sees it when picking photos to add to the spot.
// IsOnSpot: the photo is already one of the spot's own photos.
public record AdminSpotCheckInResponse(
    Guid Id,
    Guid UserId,
    string UserDisplayName,
    string PhotoUrl,
    DateTimeOffset CreatedAt,
    bool IsOnSpot);
