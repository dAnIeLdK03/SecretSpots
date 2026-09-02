namespace SecretSpots.Features.Spots;

public record NearbySpotsResponse(IReadOnlyList<NearbySpotResponse> Items, int TotalCount);
