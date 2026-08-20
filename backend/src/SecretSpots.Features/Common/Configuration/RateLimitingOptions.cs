namespace SecretSpots.Features.Common.Configuration;

public static class RateLimitPolicies
{
    public const string Auth = "auth";
    public const string Photos = "photos";
}

public class RateLimitingOptions
{
    public int GlobalPermitLimit { get; set; } = 100;
    public int GlobalWindowSeconds { get; set; } = 60;

    public int AuthPermitLimit { get; set; } = 10;
    public int AuthWindowSeconds { get; set; } = 60;

    // Each upload decodes, resizes and re-encodes an image plus a network write to R2 — far
    // heavier per-request than a typical JSON endpoint, and it's the only endpoint that writes
    // to paid storage. Tighter than the global default so upload abuse can't hide inside it.
    public int PhotosPermitLimit { get; set; } = 20;
    public int PhotosWindowSeconds { get; set; } = 60;
}
