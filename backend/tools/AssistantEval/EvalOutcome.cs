namespace AssistantEval;

// What one question produced, and how it measured up to what the case expects.
public record EvalOutcome(
    EvalCase Case,
    string? Answer,
    // Set instead of Answer when the assistant failed (limit, disabled, model error).
    string? Failure,
    IReadOnlyList<string> CitedSpots,
    IReadOnlyList<string> Trace,
    TimeSpan Elapsed,
    IReadOnlyList<string> Problems)
{
    public bool Passed => Problems.Count == 0;

    // Checks that need a person (wording, tone) are not scored, only flagged.
    public bool NeedsReview => Case.Review is not null;
}
