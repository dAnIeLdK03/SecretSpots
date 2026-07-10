namespace SecretSpots.Features.Spots;

// Keys into the shared SharedResources.resx / SharedResources.bg.resx pair
// (Common/Localization) — Spots keeps only the constants, not the translations.
public static class SpotsMessageKeys
{
    public const string NameRequired = "Spots.NameRequired";
    public const string NameTooLong = "Spots.NameTooLong";
    public const string DescriptionRequired = "Spots.DescriptionRequired";
    public const string DescriptionTooLong = "Spots.DescriptionTooLong";
    public const string PhotoUrlRequired = "Spots.PhotoUrlRequired";
    public const string PhotoUrlInvalid = "Spots.PhotoUrlInvalid";
    public const string InvalidCategory = "Spots.InvalidCategory";
    public const string LatitudeOutOfRange = "Spots.LatitudeOutOfRange";
    public const string LongitudeOutOfRange = "Spots.LongitudeOutOfRange";
    public const string RadiusOutOfRange = "Spots.RadiusOutOfRange";
}
