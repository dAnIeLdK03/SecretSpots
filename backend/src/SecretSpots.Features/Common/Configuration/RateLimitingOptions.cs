namespace SecretSpots.Features.Common.Configuration;

public static class RateLimitPolicies
{
    public const string Auth = "auth";
    public const string Photos = "photos";
    public const string ContentWrites = "content-writes";
    public const string Reports = "reports";
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

    // Covers comment/rating/report writes — endpoints a spammer could otherwise hammer well
    // within the generous global limit above.
    public int ContentWritesPermitLimit { get; set; } = 30;
    public int ContentWritesWindowSeconds { get; set; } = 60;

    // A report pages every admin (in-app + push) the moment it's created, unlike a comment or
    // rating — so this endpoint is a more attractive target for someone trying to spam the
    // admin queue or grief another user, and gets a much tighter limit than ContentWrites.
    public int ReportsPermitLimit { get; set; } = 5;
    public int ReportsWindowSeconds { get; set; } = 300;
}
