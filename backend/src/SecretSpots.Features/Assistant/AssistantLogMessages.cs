namespace SecretSpots.Features.Assistant;

// Log message templates only — operational/diagnostic text, not user-facing,
// so it stays a plain constant (no bg/en translation needed).
internal static class AssistantLogMessages
{
    public const string Answered =
        "Assistant answered user {UserId} in {ElapsedMs} ms: {ToolCalls} tool call(s), {SpotCount} spot(s), {InputTokens} input / {OutputTokens} output tokens.";
    public const string ModelFailed = "Assistant model call failed for user {UserId} after {ElapsedMs} ms.";
    public const string EmptyAnswer = "Assistant returned no text for user {UserId} after {ToolCalls} tool call(s).";
}
