namespace SecretSpots.Features.CheckIns;

public record CheckInResponse(
    Guid Id,
    Guid SpotId,
    string PhotoUrl,
    DateTimeOffset CreatedAt);
