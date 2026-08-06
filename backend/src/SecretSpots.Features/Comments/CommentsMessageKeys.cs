namespace SecretSpots.Features.Comments;

// Keys into the shared SharedResources.resx / SharedResources.bg.resx pair
// (Common/Localization) — Comments keeps only the constants, not the translations.
public static class CommentsMessageKeys
{
    public const string SpotNotFound = "Comments.SpotNotFound";
    public const string TextRequired = "Comments.TextRequired";
    public const string TextTooLong = "Comments.TextTooLong";
    public const string NotFound = "Comments.NotFound";
    public const string NotYourComment = "Comments.NotYourComment";
    public const string PageOutOfRange = "Comments.PageOutOfRange";
    public const string PageSizeOutOfRange = "Comments.PageSizeOutOfRange";
}
