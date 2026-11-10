namespace SecretSpots.Features.Assistant;

// Errors the tools return to the model (not shown to users as-is), so plain English constants
// rather than bg/en resources. Each one says what to do next.
internal static class AssistantToolMessages
{
    public const string UnknownCategory = "Unknown category. Use one of: {0}.";
    public const string InvalidCoordinates = "Latitude must be between -90 and 90 and longitude between -180 and 180.";
    public const string InvalidMinRating = "minRating must be between 0 and 5.";
    public const string SearchNeedsTextOrCategory = "Give search words, a category, or both.";
    public const string SpotNotFound = "No such spot. Use an id returned by search_spots or find_spots_near.";
    public const string UnknownPlace = "Unknown place. Ask the user for the nearest larger Bulgarian town instead.";
    public const string InvalidRequest = "The search parameters were not valid: {0}";
    public const string ToolLimitReached = "Tool call limit for this question reached. Answer now with what you already found.";
}
