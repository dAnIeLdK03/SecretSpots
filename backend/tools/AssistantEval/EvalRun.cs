using System.Text.Json;

namespace AssistantEval;

// The result of one model over the questions, in a form that can be saved and compared with another
// run later (a different model, or the same one after a change) without asking everything again.
public record EvalRun(string Model, DateTimeOffset At, IReadOnlyList<EvalResult> Results)
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static EvalRun From(IReadOnlyList<EvalOutcome> outcomes, string model, DateTimeOffset at) =>
        new(model, at, outcomes.Select(o => new EvalResult(
            o.Case.Id, o.Case.Group, o.Passed, o.NeedsReview, Math.Round(o.Elapsed.TotalSeconds, 1),
            o.CitedSpots, o.Problems, o.Answer, o.Failure)).ToList());

    public int Passed => Results.Count(r => r.Passed);

    public Task SaveAsync(string path) => File.WriteAllTextAsync(path, JsonSerializer.Serialize(this, Json));

    public static async Task<EvalRun> LoadAsync(string path) =>
        JsonSerializer.Deserialize<EvalRun>(await File.ReadAllTextAsync(path))
        ?? throw new InvalidDataException($"{path} is not an evaluation run.");
}

public record EvalResult(
    string Id,
    string Group,
    bool Passed,
    bool NeedsReview,
    double Seconds,
    IReadOnlyList<string> Cited,
    IReadOnlyList<string> Problems,
    string? Answer,
    string? Failure);
