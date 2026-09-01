namespace SecretSpots.Features.Common.Configuration;

public class EmailVerificationOptions
{
    // The frontend page that reads the "token" query param and confirms verification.
    public required string FrontendVerifyUrl { get; set; }
    public int TokenExpiryMinutes { get; set; } = 1440;
}
