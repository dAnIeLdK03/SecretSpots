namespace SecretSpots.Features.Businesses;

public record NearbyBusinessesResponse(IReadOnlyList<NearbyBusinessResponse> Items, int TotalCount);
