namespace SecretSpots.Features.Comments;

public record CommentResponse(
    Guid Id,
    Guid SpotId,
    Guid UserId,
    string AuthorDisplayName,
    string Text,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
