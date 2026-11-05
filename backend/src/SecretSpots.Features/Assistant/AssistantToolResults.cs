namespace SecretSpots.Features.Assistant;

// What the assistant's tools hand back to the model: only what it needs to recommend a spot,
// never internal fields or anyone's account data (a comment carries only its author's display name).
// Error is set instead of throwing, so the model can correct its call or tell the user.

public record AssistantSpot(
    Guid Id,
    string Name,
    string Category,
    double AverageRating,
    int RatingsCount,
    double? DistanceKm,
    string Description);

public record AssistantSpotsResult(IReadOnlyList<AssistantSpot> Spots, string? Error = null);

// Coordinates included so the model can follow up with "something else near this one".
public record AssistantSpotDetailsResult(AssistantSpot? Spot, double? Latitude, double? Longitude, string? Error = null);

public record AssistantComment(string AuthorDisplayName, string Text, DateTimeOffset PostedAt);

public record AssistantCommentsResult(IReadOnlyList<AssistantComment> Comments, int TotalCount, string? Error = null);

public record AssistantPlaceResult(string? Name, double? Latitude, double? Longitude, string? Error = null);
