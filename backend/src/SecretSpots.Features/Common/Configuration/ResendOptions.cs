namespace SecretSpots.Features.Common.Configuration;

public class ResendOptions
{
    // Secret — set via dotnet user-secrets, never committed (see CONTRIBUTING.md).
    public required string ApiKey { get; set; }
    public required string FromEmail { get; set; }
}
