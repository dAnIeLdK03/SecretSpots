namespace SecretSpots.Features.Common.Validation;

// Keys into the shared SharedResources.resx / SharedResources.bg.resx pair — shared across any
// feature that validates raw lat/lng input (Spots, CheckIns, ...).
public static class GeoMessageKeys
{
    public const string LatitudeOutOfRange = "Geo.LatitudeOutOfRange";
    public const string LongitudeOutOfRange = "Geo.LongitudeOutOfRange";
}
