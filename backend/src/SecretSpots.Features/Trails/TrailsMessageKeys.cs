namespace SecretSpots.Features.Trails;

// Keys into the shared SharedResources.resx / SharedResources.bg.resx pair
// (Common/Localization) — Trails keeps only the constants, not the translations.
public static class TrailsMessageKeys
{
    public const string NameRequired = "Trails.NameRequired";
    public const string NameTooLong = "Trails.NameTooLong";
    public const string DescriptionRequired = "Trails.DescriptionRequired";
    public const string DescriptionTooLong = "Trails.DescriptionTooLong";
    public const string PhotoUrlRequired = "Trails.PhotoUrlRequired";
    public const string PhotoUrlInvalid = "Trails.PhotoUrlInvalid";
    public const string PhotoUrlsTooMany = "Trails.PhotoUrlsTooMany";
    public const string PointsRequired = "Trails.PointsRequired";
    public const string PointsTooMany = "Trails.PointsTooMany";
    public const string RadiusOutOfRange = "Trails.RadiusOutOfRange";
    public const string NotFound = "Trails.NotFound";
    public const string NotYourTrail = "Trails.NotYourTrail";
}
