using System.Text;

namespace AssistantEval;

public static class EvalReport
{
    public static string Line(EvalOutcome o) =>
        $"{(o.Passed ? "PASS" : "FAIL")}{(o.NeedsReview ? " (review)" : string.Empty),-9} {o.Case.Id,-26} {o.Elapsed.TotalSeconds,5:0.0}s  cited: {Cited(o)}";

    public static string Summary(IReadOnlyList<EvalOutcome> outcomes)
    {
        var text = new StringBuilder();
        foreach (var group in outcomes.GroupBy(o => o.Case.Group))
        {
            text.AppendLine($"{group.Key,-12} {group.Count(o => o.Passed)}/{group.Count()}");
        }

        text.AppendLine($"{"total",-12} {outcomes.Count(o => o.Passed)}/{outcomes.Count}");
        return text.ToString();
    }

    public static string Markdown(IReadOnlyList<EvalOutcome> outcomes, string model, DateTimeOffset at)
    {
        var text = new StringBuilder();
        text.AppendLine($"# Assistant evaluation — {model}");
        text.AppendLine();
        text.AppendLine($"Run at {at:yyyy-MM-dd HH:mm} UTC. {outcomes.Count(o => o.Passed)}/{outcomes.Count} passed the automatic checks; "
            + $"{outcomes.Count(o => o.NeedsReview)} also need a look by eye (marked \"review\").");
        text.AppendLine();
        text.AppendLine("| Case | Result | Seconds | Cited spots | Problems |");
        text.AppendLine("|---|---|---|---|---|");
        foreach (var o in outcomes)
        {
            text.AppendLine($"| {o.Case.Id} | {(o.Passed ? "pass" : "FAIL")}{(o.NeedsReview ? " (review)" : string.Empty)} | {o.Elapsed.TotalSeconds:0.0} | {Cited(o)} | {Cell(string.Join("; ", o.Problems))} |");
        }

        text.AppendLine();
        text.AppendLine("## Answers");
        foreach (var o in outcomes)
        {
            text.AppendLine();
            text.AppendLine($"### {o.Case.Id} — {(o.Passed ? "pass" : "FAIL")}");
            text.AppendLine();
            text.AppendLine($"Question ({o.Case.Locale}): {o.Case.Question}");
            if (o.Case.Review is not null)
            {
                text.AppendLine();
                text.AppendLine($"Look for: {o.Case.Review}");
            }

            text.AppendLine();
            text.AppendLine($"Steps: {(o.Trace.Count == 0 ? "none" : string.Join(" → ", o.Trace))}");
            text.AppendLine();
            text.AppendLine(o.Failure is null ? $"> {(o.Answer ?? string.Empty).Replace("\n", "\n> ")}" : $"Failed: {o.Failure}");
            foreach (var problem in o.Problems)
            {
                text.AppendLine();
                text.AppendLine($"- {problem}");
            }
        }

        return text.ToString();
    }

    private static string Cited(EvalOutcome o) => o.CitedSpots.Count == 0 ? "none" : string.Join(", ", o.CitedSpots);

    private static string Cell(string value) => value.Replace("|", "\\|");
}
