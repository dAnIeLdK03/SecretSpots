namespace SecretSpots.Features.Common.Configuration;

public class WebPushOptions
{
    public required string VapidPublicKey { get; set; }
    public required string VapidPrivateKey { get; set; }

    // A contact URI (mailto: or https:) required by the Web Push protocol so a push service can
    // reach the sender if it needs to — e.g. about an app sending excessive/abusive push traffic.
    public required string VapidSubject { get; set; }
}
