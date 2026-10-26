namespace SecretSpots.Features.SampleData;

// Log message templates only — operational/diagnostic text, not user-facing,
// so it stays a plain constant (no bg/en translation needed).
internal static class SampleDataLogMessages
{
    public const string AlreadySeeded = "Sample data already present (users on @{EmailDomain}) — skipping.";
    public const string Seeded =
        "Seeded sample data: {UserCount} users, {SpotCount} spots, {RatingCount} ratings, {CommentCount} comments, {CheckInCount} check-ins, {SavedCount} saved spots. Password for every sample account: {Password}";
    public const string SeedFailed = "Seeding sample data failed — the app keeps running without it.";
}
