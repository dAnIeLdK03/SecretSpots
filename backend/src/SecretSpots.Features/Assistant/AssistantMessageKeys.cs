namespace SecretSpots.Features.Assistant;

// Keys into the shared SharedResources.resx / SharedResources.bg.resx pair — the user-facing
// errors and validation messages of the assistant endpoint.
public static class AssistantMessageKeys
{
    public const string Disabled = "Assistant.Disabled";
    public const string DailyLimitReached = "Assistant.DailyLimitReached";
    public const string ServiceLimitReached = "Assistant.ServiceLimitReached";
    public const string Unavailable = "Assistant.Unavailable";
    public const string NoAnswer = "Assistant.NoAnswer";
    public const string QuestionRequired = "Assistant.QuestionRequired";
    public const string QuestionTooLong = "Assistant.QuestionTooLong";
    public const string HistoryTooLong = "Assistant.HistoryTooLong";
    public const string HistoryInvalid = "Assistant.HistoryInvalid";
    public const string LocaleInvalid = "Assistant.LocaleInvalid";
    public const string LocationInvalid = "Assistant.LocationInvalid";
}
