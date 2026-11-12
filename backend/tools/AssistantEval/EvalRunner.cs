using System.Diagnostics;
using SecretSpots.Features.Assistant;

namespace AssistantEval;

// Asks one question through the real handler, tools and database, and checks the result.
public class EvalRunner(AskAssistant.Handler handler, IReadOnlyDictionary<string, Guid> spotIds)
{
    public async Task<EvalOutcome> RunAsync(EvalCase evalCase)
    {
        var history = evalCase.History?.Select(t => new AskAssistant.HistoryMessage(t.Role, t.Text)).ToList();
        var command = new AskAssistant.Command(evalCase.Question, history, evalCase.Locale, evalCase.Latitude, evalCase.Longitude);
        var stopwatch = Stopwatch.StartNew();

        string? answer = null, failure = null;
        IReadOnlyList<Guid> citedIds = [];
        var trace = new List<string>();

        var started = await handler.Handle(command, CancellationToken.None);
        if (!started.IsSuccess)
        {
            failure = $"{started.Error.Code}: {started.Error.Message}";
        }
        else
        {
            await foreach (var assistantEvent in started.Value)
            {
                switch (assistantEvent.Type)
                {
                    case AssistantEvent.StatusType:
                        trace.Add(assistantEvent.Text!);
                        break;
                    case AssistantEvent.DoneType:
                        answer = assistantEvent.Text;
                        citedIds = assistantEvent.SpotIds ?? [];
                        break;
                    case AssistantEvent.ErrorType:
                        failure = $"{assistantEvent.Code}: {assistantEvent.Text}";
                        break;
                }
            }
        }

        stopwatch.Stop();
        var keyById = spotIds.ToDictionary(p => p.Value, p => p.Key);
        var cited = citedIds.Select(id => keyById.GetValueOrDefault(id, $"(not an eval spot) {id}")).ToList();
        return new EvalOutcome(evalCase, answer, failure, cited, trace, stopwatch.Elapsed, Check(evalCase, answer, failure, cited));
    }

    private static List<string> Check(EvalCase evalCase, string? answer, string? failure, IReadOnlyList<string> cited)
    {
        var problems = new List<string>();
        if (failure is not null)
        {
            problems.Add($"The assistant failed: {failure}");
            return problems;
        }

        foreach (var key in evalCase.Expect.Where(key => !cited.Contains(key)))
        {
            problems.Add($"Missing expected spot: {key}");
        }

        foreach (var key in evalCase.Forbid.Where(cited.Contains))
        {
            problems.Add($"Cited a spot it must not: {key}");
        }

        if (evalCase.ExpectNoSpots && cited.Count > 0)
        {
            problems.Add($"Expected no spots, cited: {string.Join(", ", cited)}");
        }

        foreach (var text in evalCase.MustNotContain ?? [])
        {
            if (answer!.Contains(text, StringComparison.OrdinalIgnoreCase))
            {
                problems.Add($"The answer contains \"{text}\"");
            }
        }

        return problems;
    }
}
