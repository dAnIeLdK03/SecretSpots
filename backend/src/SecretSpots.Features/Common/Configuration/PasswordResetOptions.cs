namespace SecretSpots.Features.Common.Configuration;

public class PasswordResetOptions
{
    // The frontend page that reads the "token" query param and lets the user set a new password.
    public required string FrontendResetUrl { get; set; }
    public int TokenExpiryMinutes { get; set; } = 30;
}
