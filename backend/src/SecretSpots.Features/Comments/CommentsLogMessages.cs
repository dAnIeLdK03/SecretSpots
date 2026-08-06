namespace SecretSpots.Features.Comments;

// Internal structured-log templates — not user-facing, so no localization.
public static class CommentsLogMessages
{
    public const string CommentCreated = "Comment {CommentId} created on spot {SpotId} by user {UserId}";
    public const string CommentUpdated = "Comment {CommentId} updated by user {UserId}";
    public const string CommentDeleted = "Comment {CommentId} deleted by user {UserId}";
}
