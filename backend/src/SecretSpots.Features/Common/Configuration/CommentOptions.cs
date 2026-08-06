namespace SecretSpots.Features.Common.Configuration;

public class CommentOptions
{
    public int MaxTextLength { get; set; } = 1000;
    public int DefaultPageSize { get; set; } = 20;
    public int MaxPageSize { get; set; } = 100;
}
