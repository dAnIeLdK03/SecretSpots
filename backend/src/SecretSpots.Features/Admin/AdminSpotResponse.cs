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
    DateTimeOffset? ScheduledDeletionAt);
