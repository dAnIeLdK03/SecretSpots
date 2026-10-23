using SecretSpots.Domain;

namespace SecretSpots.Features.Admin;

// The spot as it is now alongside what the creator proposes, so the admin can compare them.
public record AdminSpotChangeRequestResponse(
    Guid Id,
    SpotChangeRequestType Type,
    DateTimeOffset CreatedAt,
    Guid RequestedByUserId,
    string RequestedByDisplayName,
    Guid SpotId,
    string CurrentName,
    string CurrentDescription,
    SpotCategory CurrentCategory,
    IReadOnlyList<string> CurrentPhotoUrls,
    string? ProposedName,
    string? ProposedDescription,
    SpotCategory? ProposedCategory,
    IReadOnlyList<string>? ProposedPhotoUrls);
