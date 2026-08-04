namespace SecretSpots.Features.Common.Configuration;

public class ExternalAuthOptions
{
    public required string ApiBaseUrl { get; set; }
    public required string FrontendCallbackUrl { get; set; }
}