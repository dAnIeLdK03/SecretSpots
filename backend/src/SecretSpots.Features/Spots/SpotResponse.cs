using SecretSpots.Domain;

namespace SecretSpots.Features.Spots;

public record SpotResponse(
    Guid Id,
    string Name,
    string Description,
    SpotCategory Category,
    IReadOnlyList<string> PhotoUrls,
    double Latitude,
    double Longitude,
    Guid CreatedByUserId,
    string CreatedByDisplayName,
    double AverageRating,
    int RatingsCount,
    DateTimeOffset CreatedAt,
    SpotModerationStatus ModerationStatus,
    string? RejectionReason,
    DateTimeOffset? ScheduledDeletionAt)
{
    public static SpotResponse From(Spot spot, string creatorDisplayName) => new(
        spot.Id,
        spot.Name,
        spot.Description,
        spot.Category,
        spot.PhotoUrls,
        spot.Location.Y,
        spot.Location.X,
        spot.CreatedByUserId,
        creatorDisplayName,
        spot.AverageRating,
        spot.RatingsCount,
        spot.CreatedAt,
        spot.ModerationStatus,
        spot.RejectionReason,
        spot.ScheduledDeletionAt);
}
