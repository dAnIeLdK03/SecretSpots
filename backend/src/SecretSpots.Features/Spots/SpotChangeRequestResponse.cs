using SecretSpots.Domain;

namespace SecretSpots.Features.Spots;

public record SpotChangeRequestResponse(
    Guid Id,
    Guid SpotId,
    SpotChangeRequestType Type,
    SpotChangeRequestStatus Status,
    string? ProposedName,
    string? ProposedDescription,
    SpotCategory? ProposedCategory,
    IReadOnlyList<string>? ProposedPhotoUrls,
    string? RejectionReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReviewedAt)
{
    public static SpotChangeRequestResponse From(SpotChangeRequest request) => new(
        request.Id,
        request.SpotId,
        request.Type,
        request.Status,
        request.ProposedName,
        request.ProposedDescription,
        request.ProposedCategory,
        request.ProposedPhotoUrls,
        request.RejectionReason,
        request.CreatedAt,
        request.ReviewedAt);
}
