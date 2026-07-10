namespace SecretSpots.Features.CheckIns;

// Log message templates only — operational/diagnostic text, not user-facing,
// so it stays a plain constant (no bg/en translation needed, unlike CheckInsMessageKeys).
internal static class CheckInsLogMessages
{
    public const string CheckInCreated =
        "CheckIn {CheckInId} at spot {SpotId} by user {UserId} awarded {CrystalsAwarded} crystals.";
}
