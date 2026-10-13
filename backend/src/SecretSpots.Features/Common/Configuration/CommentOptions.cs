namespace SecretSpots.Features.Common.Configuration;

public class CommentOptions : PaginationOptionsBase
{
    public int MaxTextLength { get; set; } = 1000;
}
