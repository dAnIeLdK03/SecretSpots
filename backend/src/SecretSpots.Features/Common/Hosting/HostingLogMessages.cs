namespace SecretSpots.Features.Common.Hosting;

// Log message templates only — operational/diagnostic text, not user-facing,
// so it stays a plain constant (no bg/en translation needed).
internal static class HostingLogMessages
{
    public const string PeriodicRunFailed = "{Service} run failed; it will try again on its next interval.";
}
