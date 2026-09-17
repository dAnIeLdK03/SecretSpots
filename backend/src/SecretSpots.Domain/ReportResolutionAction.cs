namespace SecretSpots.Domain;

// Append-only — stored as int in the DB, so existing members must keep their position/value.
public enum ReportResolutionAction
{
    Dismissed,
    ContentDeleted,

    // The reported content's own author deleted it themselves (self-service spot/account/comment
    // deletion) before a moderator acted — distinct from ContentDeleted (an admin's decision) so
    // the report queue doesn't misattribute this to moderation. ResolvedByUserId is left null for
    // this action; nobody administratively resolved it.
    ContentDeletedByAuthor,
}
