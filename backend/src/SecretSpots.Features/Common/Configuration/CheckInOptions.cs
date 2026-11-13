namespace SecretSpots.Features.Common.Configuration;

public class CheckInOptions : PaginationOptionsBase
{
    public double MaxDistanceMeters { get; set; } = 75;

    // Minimum time a user must wait between check-ins at the *same* spot — stops a spot's
    // visits being inflated by repeatedly checking in at one location.
    public int CooldownHours { get; set; } = 24;
}
