using System.ComponentModel;
using FluentValidation;
using SecretSpots.Domain;
using SecretSpots.Features.Comments;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Spots;

namespace SecretSpots.Features.Assistant;

// The read-only tools the spot-discovery assistant can call. Each one goes through the regular
// handlers via ISender, so it sees exactly what the signed-in user would see — the same
// visibility rules, nothing extra — and none of them can change anything.
//
// Everything a tool receives comes from the model and is treated as untrusted input: validated or
// clamped here, with mistakes reported back through Error rather than thrown. Only Approved spots
// are ever returned: a spot no admin has reviewed yet may be spam or written to steer the model.
public class AssistantTools(ISender sender)
{
    public const int MaxSpots = 10;
    public const int MaxComments = 5;
    public const int MaxDescriptionLength = 300;
    public const int MaxCommentLength = 300;
    public const double MinRadiusKm = 1;
    public const double MaxRadiusKm = 99;
    private const int MaxSearchTextLength = 200;

    // How many candidates to read before dropping unapproved/low-rated ones and keeping MaxSpots.
    private const int CandidatePageSize = 50;

    [Description("Finds spots whose name or description matches the text (tolerates typos). Use it for what kind of place the user wants, e.g. \"waterfall\", \"quiet lake\".")]
    public async Task<AssistantSpotsResult> SearchSpotsAsync(
        [Description("Words describing the place, in Bulgarian or English.")] string text,
        [Description("Optional category name to narrow the search.")] string? category = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseCategory(category, out var spotCategory, out var categoryError))
        {
            return new AssistantSpotsResult([], categoryError);
        }

        var searchText = text.Trim();
        if (searchText.Length > MaxSearchTextLength)
        {
            searchText = searchText[..MaxSearchTextLength];
        }

        var page = await sender.Send(
            new SearchSpots.Query(searchText, spotCategory, 1, CandidatePageSize), cancellationToken);

        var spots = page.Items
            .Where(s => s.ModerationStatus == SpotModerationStatus.Approved)
            .Take(MaxSpots)
            .Select(s => ToAssistantSpot(s.Id, s.Name, s.Category, s.AverageRating, s.RatingsCount, null, s.Description))
            .ToList();
        return new AssistantSpotsResult(spots);
    }

    [Description("Finds spots within a radius of a point, nearest first. Use locate_place first to turn a town name into coordinates.")]
    public async Task<AssistantSpotsResult> FindSpotsNearAsync(
        [Description("Latitude of the centre point.")] double latitude,
        [Description("Longitude of the centre point.")] double longitude,
        [Description("Search radius in kilometres, 1 to 99.")] double radiusKm,
        [Description("Optional category name to narrow the search.")] string? category = null,
        [Description("Optional minimum average rating, 0 to 5. Spots without ratings are left out when this is set.")] double? minRating = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidCoordinate(latitude, 90) || !IsValidCoordinate(longitude, 180))
        {
            return new AssistantSpotsResult([], AssistantToolMessages.InvalidCoordinates);
        }

        if (minRating is { } rating && (double.IsNaN(rating) || rating < 0 || rating > 5))
        {
            return new AssistantSpotsResult([], AssistantToolMessages.InvalidMinRating);
        }

        if (!TryParseCategory(category, out var spotCategory, out var categoryError))
        {
            return new AssistantSpotsResult([], categoryError);
        }

        var radius = double.IsNaN(radiusKm) ? MinRadiusKm : Math.Clamp(radiusKm, MinRadiusKm, MaxRadiusKm);

        NearbySpotsResponse nearby;
        try
        {
            nearby = await sender.Send(new SearchNearbySpots.Query(latitude, longitude, radius, spotCategory), cancellationToken);
        }
        catch (ValidationException ex)
        {
            return new AssistantSpotsResult([], string.Format(AssistantToolMessages.InvalidRequest, ex.Errors.First().ErrorMessage));
        }

        var spots = nearby.Items
            .Where(s => s.ModerationStatus == SpotModerationStatus.Approved)
            .Where(s => minRating is null || (s.RatingsCount > 0 && s.AverageRating >= minRating))
            .Take(MaxSpots)
            .Select(s => ToAssistantSpot(s.Id, s.Name, s.Category, s.AverageRating, s.RatingsCount, s.DistanceKm, s.Description))
            .ToList();
        return new AssistantSpotsResult(spots);
    }

    [Description("Gets one spot by its id, including its coordinates.")]
    public async Task<AssistantSpotDetailsResult> GetSpotAsync(
        [Description("The spot's id, as returned by search_spots or find_spots_near.")] Guid spotId,
        CancellationToken cancellationToken = default)
    {
        var spot = await LoadApprovedSpotAsync(spotId, cancellationToken);
        if (spot is null)
        {
            return new AssistantSpotDetailsResult(null, null, null, AssistantToolMessages.SpotNotFound);
        }

        return new AssistantSpotDetailsResult(
            ToAssistantSpot(spot.Id, spot.Name, spot.Category, spot.AverageRating, spot.RatingsCount, null, spot.Description),
            spot.Latitude,
            spot.Longitude);
    }

    [Description("Gets the most recent visitor comments about a spot (what it's really like: crowds, paths, parking).")]
    public async Task<AssistantCommentsResult> GetSpotCommentsAsync(
        [Description("The spot's id, as returned by search_spots or find_spots_near.")] Guid spotId,
        CancellationToken cancellationToken = default)
    {
        // GetSpotComments doesn't check the spot's own visibility, so do it here: comments on a
        // rejected or unreviewed spot stay out of the assistant just like the spot itself.
        if (await LoadApprovedSpotAsync(spotId, cancellationToken) is null)
        {
            return new AssistantCommentsResult([], 0, AssistantToolMessages.SpotNotFound);
        }

        var page = await sender.Send(new GetSpotComments.Query(spotId, 1, MaxComments), cancellationToken);
        var comments = page.Items
            .Select(c => new AssistantComment(c.AuthorDisplayName, Truncate(c.Text, MaxCommentLength), c.CreatedAt))
            .ToList();
        return new AssistantCommentsResult(comments, page.TotalCount);
    }

    [Description("Turns the name of a Bulgarian town into coordinates, e.g. \"Plovdiv\" or \"гр. Пловдив\".")]
    public Task<AssistantPlaceResult> LocatePlaceAsync(
        [Description("The town's name, in Bulgarian or Latin letters.")] string name)
    {
        var place = BulgarianPlaces.Find(name);
        return Task.FromResult(place is null
            ? new AssistantPlaceResult(null, null, null, AssistantToolMessages.UnknownPlace)
            : new AssistantPlaceResult(place.Name, place.Latitude, place.Longitude));
    }

    private async Task<SpotResponse?> LoadApprovedSpotAsync(Guid spotId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSpot.Query(spotId), cancellationToken);
        return result.IsSuccess && result.Value.ModerationStatus == SpotModerationStatus.Approved ? result.Value : null;
    }

    private static bool IsValidCoordinate(double value, double limit) => !double.IsNaN(value) && value >= -limit && value <= limit;

    // Only category names (case-insensitive); a number would also parse as an enum value, so it's refused.
    private static bool TryParseCategory(string? category, out SpotCategory? spotCategory, out string? error)
    {
        spotCategory = null;
        error = null;
        if (string.IsNullOrWhiteSpace(category))
        {
            return true;
        }

        var trimmed = category.Trim();
        if (!int.TryParse(trimmed, out _) && Enum.TryParse<SpotCategory>(trimmed, ignoreCase: true, out var parsed))
        {
            spotCategory = parsed;
            return true;
        }

        error = string.Format(AssistantToolMessages.UnknownCategory, string.Join(", ", Enum.GetNames<SpotCategory>()));
        return false;
    }

    // Rounded the way people expect (4.25 -> 4.3), not .NET's default banker's rounding (4.25 -> 4.2).
    private static AssistantSpot ToAssistantSpot(
        Guid id, string name, SpotCategory category, double averageRating, int ratingsCount, double? distanceKm, string description) =>
        new(
            id,
            name,
            category.ToString(),
            Math.Round(averageRating, 1, MidpointRounding.AwayFromZero),
            ratingsCount,
            distanceKm is { } km ? Math.Round(km, 1, MidpointRounding.AwayFromZero) : null,
            Truncate(description, MaxDescriptionLength));

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : string.Concat(text.AsSpan(0, maxLength - 1).TrimEnd(), "…");
}
