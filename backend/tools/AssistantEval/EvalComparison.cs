using System.Text;

namespace AssistantEval;

// Lines up several runs question by question. Only the cases every run asked are compared, so runs
// made with --only still line up with full ones.
public static class EvalComparison
{
    public static string Summary(IReadOnlyList<EvalRun> runs)
    {
        var ids = CommonIds(runs);
        var text = new StringBuilder();
        foreach (var run in runs)
        {
            var results = run.Results.Where(r => ids.Contains(r.Id)).ToList();
            text.AppendLine($"{run.Model,-40} {results.Count(r => r.Passed)}/{results.Count} passed, {results.Sum(r => r.Seconds):0}s in total");
        }

        return text.ToString();
    }

    public static string Markdown(IReadOnlyList<EvalRun> runs)
    {
        var ids = CommonIds(runs);
        var text = new StringBuilder();
        text.AppendLine("# Assistant models compared");
        text.AppendLine();
        text.AppendLine($"{ids.Count} questions asked in every run. Runs: "
            + string.Join("; ", runs.Select(r => $"{r.Model} ({r.At:yyyy-MM-dd HH:mm} UTC)")) + ".");

        text.AppendLine();
        text.AppendLine("## Passed, by group");
        text.AppendLine();
        text.AppendLine($"| Group | {string.Join(" | ", runs.Select(r => r.Model))} |");
        text.AppendLine($"|---|{string.Concat(runs.Select(_ => "---|"))}");
        foreach (var group in runs[0].Results.Where(r => ids.Contains(r.Id)).Select(r => r.Group).Distinct())
        {
            var cells = runs.Select(run =>
            {
                var inGroup = run.Results.Where(r => ids.Contains(r.Id) && r.Group == group).ToList();
                return $"{inGroup.Count(r => r.Passed)}/{inGroup.Count}";
            });
            text.AppendLine($"| {group} | {string.Join(" | ", cells)} |");
        }

        var totals = runs.Select(run => $"**{run.Results.Count(r => ids.Contains(r.Id) && r.Passed)}/{ids.Count}**");
        text.AppendLine($"| **total** | {string.Join(" | ", totals)} |");
        var seconds = runs.Select(run => $"{run.Results.Where(r => ids.Contains(r.Id)).Sum(r => r.Seconds):0}s");
        text.AppendLine($"| time | {string.Join(" | ", seconds)} |");

        text.AppendLine();
        text.AppendLine("## Question by question");
        text.AppendLine();
        text.AppendLine($"| Question | {string.Join(" | ", runs.Select(r => r.Model))} |");
        text.AppendLine($"|---|{string.Concat(runs.Select(_ => "---|"))}");
        foreach (var id in ids)
        {
            var cells = runs.Select(run =>
            {
                var result = run.Results.First(r => r.Id == id);
                return $"{(result.Passed ? "pass" : "FAIL")}{(result.NeedsReview ? " (review)" : string.Empty)} {result.Seconds:0.0}s";
            });
            text.AppendLine($"| {id} | {string.Join(" | ", cells)} |");
        }

        var differing = ids.Where(id => runs.Select(run => run.Results.First(r => r.Id == id).Passed).Distinct().Count() > 1).ToList();
        text.AppendLine();
        text.AppendLine("## Where the runs disagree");
        if (differing.Count == 0)
        {
            text.AppendLine();
            text.AppendLine("Every run passed and failed the same questions.");
        }

        foreach (var id in differing)
        {
            text.AppendLine();
            text.AppendLine($"### {id}");
            foreach (var run in runs)
            {
                var result = run.Results.First(r => r.Id == id);
                text.AppendLine();
                text.AppendLine($"- {run.Model}: {(result.Passed ? "pass" : "FAIL")}, cited {(result.Cited.Count == 0 ? "none" : string.Join(", ", result.Cited))}"
                    + (result.Problems.Count > 0 ? $" — {string.Join("; ", result.Problems)}" : string.Empty));
            }
        }

        return text.ToString();
    }

    private static List<string> CommonIds(IReadOnlyList<EvalRun> runs) =>
        runs[0].Results.Select(r => r.Id).Where(id => runs.All(run => run.Results.Any(r => r.Id == id))).ToList();
}
