namespace SecretSpots.Features.Common.Configuration;

public class SpotModerationOptions
{
    // How long a rejected spot stays visible to its creator before it's soft-deleted.
    public int RejectionGracePeriodHours { get; set; } = 24;

    public int CleanupIntervalMinutes { get; set; } = 60;
}
