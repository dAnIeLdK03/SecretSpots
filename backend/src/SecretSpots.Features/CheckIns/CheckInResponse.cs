namespace SecretSpots.Features.CheckIns;

public record CheckInResponse(
    Guid Id,
    Guid SpotId,
    string PhotoUrl,
    int CrystalsAwarded,
    int NewCrystalBalance,
    DateTimeOffset CreatedAt);
