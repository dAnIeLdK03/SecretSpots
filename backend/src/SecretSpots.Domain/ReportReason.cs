namespace SecretSpots.Domain;

// Append-only — stored as int in the DB, so existing members must keep their position/value.
public enum ReportReason
{
    Spam,
    Inappropriate,
    Fake,
    Other,
}
