using SecretSpots.Domain;

namespace SecretSpots.Features.Admin;

// SpotSearchResultResponse plus the creator's display name, which only the admin moderation
// views need.
public record AdminSpotResponse(
    Guid Id,
    string Name,
    string Description,
    SpotCategory Category,
    string PhotoUrl,
    double Latitude,
    double Longitude,
    Guid CreatedByUserId,
    string CreatedByDisplayName,
    DateTimeOffset CreatedAt,
    SpotModerationStatus ModerationStatus,
    string? RejectionReason,
    DateTimeOffset? ScheduledDeletionAt)
{
    // Same in-memory-only caveat as SpotSearchResultResponse.From.
    public static AdminSpotResponse From(Spot spot, string creatorDisplayName) => new(
        spot.Id,
        spot.Name,
        spot.Description,
        spot.Category,
        spot.PhotoUrls[0],
        spot.Location.Y,
        spot.Location.X,
        spot.CreatedByUserId,
        creatorDisplayName,
        spot.CreatedAt,
        spot.ModerationStatus,
        spot.RejectionReason,
        spot.ScheduledDeletionAt);
}
