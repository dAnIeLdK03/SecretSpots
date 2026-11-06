using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Localization;
using SecretSpots.Features.Common.Localization;

namespace SecretSpots.Features.Assistant;

// Turns the model's request to use a tool into a progress line for the user ("Looking for spots
// around Plovdiv…"). The arguments come from the model, so text taken from them is shortened, and
// spots and towns are named only from what the tools themselves returned earlier.
public static class AssistantStatusMessages
{
    private const int MaxArgumentLength = 60;

    public static string For(FunctionCallContent call, AssistantToolset toolset, IStringLocalizer<SharedResources> localizer)
    {
        var arguments = call.Arguments;
        switch (call.Name)
        {
            case AssistantToolset.SearchSpots when Text(arguments, "text") is { } text:
                return localizer[AssistantMessageKeys.StatusSearching, text].Value;

            case AssistantToolset.FindSpotsNear:
                var place = Number(arguments, "latitude") is { } latitude && Number(arguments, "longitude") is { } longitude
                    ? toolset.LocatedPlaceAt(latitude, longitude)
                    : null;
                return place is null
                    ? localizer[AssistantMessageKeys.StatusSearchingNearby].Value
                    : localizer[AssistantMessageKeys.StatusSearchingNearPlace, place].Value;

            case AssistantToolset.GetSpot:
                return SpotName(arguments, toolset) is { } spot
                    ? localizer[AssistantMessageKeys.StatusReadingSpot, spot].Value
                    : localizer[AssistantMessageKeys.StatusReadingSpotUnnamed].Value;

            case AssistantToolset.GetSpotComments:
                return SpotName(arguments, toolset) is { } commented
                    ? localizer[AssistantMessageKeys.StatusReadingComments, commented].Value
                    : localizer[AssistantMessageKeys.StatusReadingCommentsUnnamed].Value;

            case AssistantToolset.LocatePlace when Text(arguments, "name") is { } name:
                return localizer[AssistantMessageKeys.StatusLocating, name].Value;

            default:
                return localizer[AssistantMessageKeys.StatusThinking].Value;
        }
    }

    private static string? SpotName(IDictionary<string, object?>? arguments, AssistantToolset toolset) =>
        Guid.TryParse(Text(arguments, "spotId"), out var spotId) ? toolset.SpotName(spotId) : null;

    // Argument values arrive as JsonElement from a real model and as plain values from tests.
    private static string? Text(IDictionary<string, object?>? arguments, string name)
    {
        var raw = Value(arguments, name) switch
        {
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            string text => text,
            _ => null,
        };
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();
        return trimmed.Length <= MaxArgumentLength ? trimmed : trimmed[..(MaxArgumentLength - 1)] + "…";
    }

    private static double? Number(IDictionary<string, object?>? arguments, string name) => Value(arguments, name) switch
    {
        JsonElement { ValueKind: JsonValueKind.Number } element => element.GetDouble(),
        JsonElement { ValueKind: JsonValueKind.String } element when double.TryParse(element.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
        double number => number,
        float number => number,
        int number => number,
        _ => null,
    };

    private static object? Value(IDictionary<string, object?>? arguments, string name) =>
        arguments?.FirstOrDefault(a => string.Equals(a.Key, name, StringComparison.OrdinalIgnoreCase)).Value;
}
