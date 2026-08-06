namespace SecretSpots.Features.Comments;

public record CommentsPageResponse(
    IReadOnlyList<CommentResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
