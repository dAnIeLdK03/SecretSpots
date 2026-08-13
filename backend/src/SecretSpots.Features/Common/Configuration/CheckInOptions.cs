namespace SecretSpots.Features.Common.Configuration;

public class CheckInOptions
{
    public double MaxDistanceMeters { get; set; } = 75;
    public int DefaultPageSize { get; set; } = 20;
    public int MaxPageSize { get; set; } = 100;

    // Minimum time a user must wait between crystal-earning check-ins at the *same* spot —
    // stops trivially farming crystals by repeatedly checking in at one location.
    public int CooldownHours { get; set; } = 24;
}
