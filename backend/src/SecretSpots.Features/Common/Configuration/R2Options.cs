namespace SecretSpots.Features.Common.Configuration;

public class R2Options
{
    public required string AccountId { get; set; }
    public required string BucketName { get; set; }
    public required string PublicBaseUrl { get; set; }

    // Secrets — set via dotnet user-secrets, never committed (see CONTRIBUTING.md).
    public required string AccessKeyId { get; set; }
    public required string SecretAccessKey { get; set; }
}
