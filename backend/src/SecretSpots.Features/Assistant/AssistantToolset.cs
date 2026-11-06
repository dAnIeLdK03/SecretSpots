using System.Text.Json;
using Microsoft.Extensions.AI;

namespace SecretSpots.Features.Assistant;

// The AssistantTools of one question, as functions the model can call. Counts the calls — past
// the limit a tool answers "limit reached, answer now" instead of running, so the model still
// finishes with a proper answer (FunctionInvokingChatClient's own iteration cap would just stop
// mid-way) — and remembers what the tools returned: every spot id, so cited ids can be checked,
// and the names of spots and located towns, so progress messages can name them.
public sealed class AssistantToolset
{
    private readonly int _maxToolCalls;
    private readonly HashSet<Guid> _returnedSpotIds = [];
    private readonly Dictionary<Guid, string> _spotNames = [];
    private readonly List<BulgarianPlaces.Place> _locatedPlaces = [];
    private int _toolCalls;

    public AssistantToolset(AssistantTools tools, int maxToolCalls)
    {
        _maxToolCalls = maxToolCalls;
        Tools =
        [
            Counted(AIFunctionFactory.Create(tools.SearchSpotsAsync, SearchSpots)),
            Counted(AIFunctionFactory.Create(tools.FindSpotsNearAsync, FindSpotsNear)),
            Counted(AIFunctionFactory.Create(tools.GetSpotAsync, GetSpot)),
            Counted(AIFunctionFactory.Create(tools.GetSpotCommentsAsync, GetSpotComments)),
            Counted(AIFunctionFactory.Create(tools.LocatePlaceAsync, LocatePlace)),
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

    public const string SearchSpots = "search_spots";
    public const string FindSpotsNear = "find_spots_near";
    public const string GetSpot = "get_spot";
    public const string GetSpotComments = "get_spot_comments";
    public const string LocatePlace = "locate_place";

    public string? SpotName(Guid spotId)
    {
        lock (_returnedSpotIds)
        {
            return _spotNames.GetValueOrDefault(spotId);
        }
    }

    // The town a nearby search is centred on, if it's one locate_place returned (the model passes
    // the coordinates back as it got them, give or take rounding).
    public string? LocatedPlaceAt(double latitude, double longitude)
    {
        lock (_returnedSpotIds)
        {
            return _locatedPlaces
                .FirstOrDefault(p => Math.Abs(p.Latitude - latitude) < 0.01 && Math.Abs(p.Longitude - longitude) < 0.01)?.Name;
        }
    }

    private AIFunction Counted(AIFunction function) => new CountedFunction(function, this);

    // Results may come back as the tool's record or already serialized to JSON, depending on how
    // the function marshals its return value — read the ids from the JSON shape either way.
    private void Record(object? result)
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
                    if (TryGetProperty(spot, "name", out var name) && name.ValueKind == JsonValueKind.String)
                    {
                        _spotNames[spotId] = name.GetString()!;
                    }
                }
            }

            if (json.ValueKind == JsonValueKind.Object
                && TryGetProperty(json, "latitude", out var latitude) && latitude.ValueKind == JsonValueKind.Number
                && TryGetProperty(json, "longitude", out var longitude) && longitude.ValueKind == JsonValueKind.Number
                && TryGetProperty(json, "name", out var placeName) && placeName.ValueKind == JsonValueKind.String)
            {
                _locatedPlaces.Add(new BulgarianPlaces.Place(placeName.GetString()!, latitude.GetDouble(), longitude.GetDouble()));
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
            owner.Record(result);
            return result;
        }
    }
}
