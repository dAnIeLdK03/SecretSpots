using System.Text.Json;
using Microsoft.Extensions.AI;

namespace SecretSpots.Features.Assistant;

// The AssistantTools of one question, as functions the model can call. Counts the calls — past
// the limit a tool answers "limit reached, answer now" instead of running, so the model still
// finishes with a proper answer (FunctionInvokingChatClient's own iteration cap would just stop
// mid-way) — and remembers every spot id the tools returned, so cited ids can be checked.
public sealed class AssistantToolset
{
    private readonly int _maxToolCalls;
    private readonly HashSet<Guid> _returnedSpotIds = [];
    private int _toolCalls;

    public AssistantToolset(AssistantTools tools, int maxToolCalls)
    {
        _maxToolCalls = maxToolCalls;
        Tools =
        [
            Counted(AIFunctionFactory.Create(tools.SearchSpotsAsync, "search_spots")),
            Counted(AIFunctionFactory.Create(tools.FindSpotsNearAsync, "find_spots_near")),
            Counted(AIFunctionFactory.Create(tools.GetSpotAsync, "get_spot")),
            Counted(AIFunctionFactory.Create(tools.GetSpotCommentsAsync, "get_spot_comments")),
            Counted(AIFunctionFactory.Create(tools.LocatePlaceAsync, "locate_place")),
        ];
    }

    public IList<AITool> Tools { get; }

    // Calls that actually ran, not the ones turned away by the limit.
    public int ToolCalls => Math.Min(_toolCalls, _maxToolCalls);

    public IReadOnlySet<Guid> ReturnedSpotIds
    {
        get
        {
            lock (_returnedSpotIds)
            {
                return _returnedSpotIds.ToHashSet();
            }
        }
    }

    private AIFunction Counted(AIFunction function) => new CountedFunction(function, this);

    // Results may come back as the tool's record or already serialized to JSON, depending on how
    // the function marshals its return value — read the ids from the JSON shape either way.
    private void RecordSpotIds(object? result)
    {
        if (result is null)
        {
            return;
        }

        var json = result is JsonElement element ? element : JsonSerializer.SerializeToElement(result, result.GetType());
        lock (_returnedSpotIds)
        {
            foreach (var spot in SpotsIn(json))
            {
                if (TryGetProperty(spot, "id", out var id) && id.ValueKind == JsonValueKind.String && Guid.TryParse(id.GetString(), out var spotId))
                {
                    _returnedSpotIds.Add(spotId);
                }
            }
        }
    }

    private static IEnumerable<JsonElement> SpotsIn(JsonElement result)
    {
        if (result.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        if (TryGetProperty(result, "spots", out var spots) && spots.ValueKind == JsonValueKind.Array)
        {
            foreach (var spot in spots.EnumerateArray())
            {
                yield return spot;
            }
        }

        if (TryGetProperty(result, "spot", out var single) && single.ValueKind == JsonValueKind.Object)
        {
            yield return single;
        }
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private sealed class CountedFunction(AIFunction inner, AssistantToolset owner) : DelegatingAIFunction(inner)
    {
        protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref owner._toolCalls) > owner._maxToolCalls)
            {
                return new { error = AssistantToolMessages.ToolLimitReached };
            }

            var result = await base.InvokeCoreAsync(arguments, cancellationToken);
            owner.RecordSpotIds(result);
            return result;
        }
    }
}
