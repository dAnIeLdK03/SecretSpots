namespace SecretSpots.Features.Reports;

// Keys into the shared SharedResources.resx / SharedResources.bg.resx pair
// (Common/Localization) — Reports keeps only the constants, not the translations.
public static class ReportsMessageKeys
{
    public const string SpotNotFound = "Reports.SpotNotFound";
    public const string CommentNotFound = "Reports.CommentNotFound";
    public const string AlreadyReported = "Reports.AlreadyReported";
    public const string ReasonRequired = "Reports.ReasonRequired";
    public const string DetailsTooLong = "Reports.DetailsTooLong";
    public const string NotFound = "Reports.NotFound";
    public const string PageOutOfRange = "Reports.PageOutOfRange";
    public const string PageSizeOutOfRange = "Reports.PageSizeOutOfRange";
}
