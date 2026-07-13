namespace SecretSpots.Features.Businesses;

public record BusinessResponse(
    Guid Id,
    string Name,
    string Description,
    double Latitude,
    double Longitude,
    Guid OwnerUserId,
    bool IsPromoted,
    DateTimeOffset CreatedAt);
