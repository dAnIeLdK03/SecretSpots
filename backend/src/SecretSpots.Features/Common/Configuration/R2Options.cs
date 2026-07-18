namespace SecretSpots.Features.Common.Configuration;

public class R2Options
{
    public required string AccountId { get; set; }
    public required string BucketName { get; set; }
    public required string PublicBaseUrl { get; set; }

    // Local dev only — points the S3 client at a self-hosted MinIO instead of
    // Cloudflare R2, so photo upload works without a Cloudflare account/card.
    // Leave unset in every real environment.
    public string? ServiceUrlOverride { get; set; }

    // Secrets — set via dotnet user-secrets, never committed (see CONTRIBUTING.md).
    public required string AccessKeyId { get; set; }
    public required string SecretAccessKey { get; set; }
}
