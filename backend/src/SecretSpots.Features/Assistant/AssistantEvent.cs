namespace SecretSpots.Features.Assistant;

// One server-sent event of an assistant answer, in order: any number of "status" (what it's doing)
// and "delta" (the next piece of the answer's text) events, then exactly one "done" (the final
// answer and the spots it recommends) or "error" (the model failed; a bg/en message for the user).
public record AssistantEvent(string Type, string? Text = null, IReadOnlyList<Guid>? SpotIds = null, string? Code = null)
{
    public const string StatusType = "status";
    public const string DeltaType = "delta";
    public const string DoneType = "done";
    public const string ErrorType = "error";

    public static AssistantEvent Status(string text) => new(StatusType, text);

    public static AssistantEvent Delta(string text) => new(DeltaType, text);

    public static AssistantEvent Done(string answer, IReadOnlyList<Guid> spotIds) => new(DoneType, answer, spotIds);

    public static AssistantEvent Error(string code, string message) => new(ErrorType, message, Code: code);
}
