using AssistantEval;

// Lists the evaluation questions. Running them against a model comes with the runner.
Console.WriteLine($"{EvalCases.All.Count} questions, {EvalSpots.All.Count} seeded spots.");
Console.WriteLine();
foreach (var group in EvalCases.All.GroupBy(c => c.Group))
{
    Console.WriteLine(group.Key);
    foreach (var c in group)
    {
        Console.WriteLine($"  [{c.Locale}] {c.Id}: {c.Question}");
    }
}
