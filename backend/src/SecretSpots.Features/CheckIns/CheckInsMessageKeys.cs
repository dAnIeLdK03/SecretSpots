namespace SecretSpots.Features.CheckIns;

// Keys into the shared SharedResources.resx / SharedResources.bg.resx pair
// (Common/Localization) — CheckIns keeps only the constants, not the translations.
public static class CheckInsMessageKeys
{
    public const string SpotNotFound = "CheckIns.SpotNotFound";
    public const string TooFarFromSpot = "CheckIns.TooFarFromSpot";
    public const string PhotoUrlRequired = "CheckIns.PhotoUrlRequired";
    public const string PhotoUrlInvalid = "CheckIns.PhotoUrlInvalid";
}
