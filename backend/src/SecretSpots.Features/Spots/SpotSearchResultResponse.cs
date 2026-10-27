using SecretSpots.Domain;

namespace SecretSpots.Features.Spots;

public record SpotSearchResultResponse(
    Guid Id,
    string Name,
    string Description,
    SpotCategory Category,
    string PhotoUrl,
    double Latitude,
    double Longitude,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    SpotModerationStatus ModerationStatus,
    string? RejectionReason,
    DateTimeOffset? ScheduledDeletionAt)
{
    // Only callable on a materialized Spot — PhotoUrls[0] and Location.Y/.X can't be translated
    // to SQL by the Npgsql provider, so every caller projects in memory.
    public static SpotSearchResultResponse From(Spot spot) => new(
        spot.Id,
        spot.Name,
        spot.Description,
        spot.Category,
        spot.PhotoUrls[0],
        spot.Location.Y,
        spot.Location.X,
        spot.CreatedByUserId,
        spot.CreatedAt,
        spot.ModerationStatus,
        spot.RejectionReason,
        spot.ScheduledDeletionAt);
}