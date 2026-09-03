namespace SecretSpots.Features.Reports;

// Internal structured-log templates — not user-facing, so no localization.
public static class ReportsLogMessages
{
    public const string ContentReported = "{ContentType} {ContentId} reported for {Reason} by user {UserId}";
    public const string ReportDismissed = "Report {ReportId} dismissed by admin {AdminUserId}";
    public const string ReportedContentDeleted =
        "{ContentType} {ContentId} deleted by admin {AdminUserId} following report {ReportId}";
}
