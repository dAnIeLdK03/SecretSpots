namespace SecretSpots.Features.CheckIns;

public record MyCheckInResponse(
    Guid Id,
    Guid SpotId,
    string SpotName,
    string PhotoUrl,
    DateTimeOffset CreatedAt);
