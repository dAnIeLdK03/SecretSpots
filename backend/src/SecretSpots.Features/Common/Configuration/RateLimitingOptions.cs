namespace SecretSpots.Features.Common.Configuration;

public static class RateLimitPolicies
{
    public const string Auth = "auth";
}

public class RateLimitingOptions
{
    public int GlobalPermitLimit { get; set; } = 100;
    public int GlobalWindowSeconds { get; set; } = 60;

    public int AuthPermitLimit { get; set; } = 10;
    public int AuthWindowSeconds { get; set; } = 60;
}
