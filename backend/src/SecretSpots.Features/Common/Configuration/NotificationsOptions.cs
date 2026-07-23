namespace SecretSpots.Features.Common.Configuration;

public class NotificationsOptions
{
    public int DefaultPageSize { get; set; } = 20;
    public int MaxPageSize { get; set; } = 100;

    // How far (in km) a user must have a prior spot or check-in from a newly created spot to get
    // a NewSpotNearby notification for it. Matches the default radius offered in the frontend's
    // nearby-search picker (page.tsx: RADIUS_OPTIONS default of 5km) for a consistent sense of "nearby".
    public double NewSpotRadiusKm { get; set; } = 5;
}
