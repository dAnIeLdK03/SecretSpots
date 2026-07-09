namespace SecretSpots.Features.Common.Security;

public class JwtOptions
{
    public required string Secret { get; set; }
    public required string Issuer { get; set; }
    public required string Audience { get; set; }
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;
}
