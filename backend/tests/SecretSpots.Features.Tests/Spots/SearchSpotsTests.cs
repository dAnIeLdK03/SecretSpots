using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Spots;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Spots;

// Text search runs over every spot in the shared test DB, so each test tags its spots with a
// unique marker word and only looks at those ids.
public class SearchSpotsTests
{
    private static Task<PagedResponse<SpotSearchResultResponse>> SearchAsync(
        IAppDbContext db, string? term, SpotCategory? category = null) =>
        new SearchSpots.Handler(db).Handle(new SearchSpots.Query(term, category, 1, 100), CancellationToken.None);

    private static string Marker() => "m" + Guid.NewGuid().ToString("N")[..12];

    [Fact]
    public async Task Finds_spots_by_name_or_description_case_insensitively()
    {
        await using var db = TestDbContextFactory.Create();
        var marker = Marker();
        var byName = await TestSpotFactory.SeedAsync(db, name: $"Hidden lake {marker}");
        var byDescription = await TestSpotFactory.SeedAsync(db);
        byDescription.Description = $"Walk past the {marker} sign";
        await db.SaveChangesAsync();

        var result = await SearchAsync(db, marker.ToUpperInvariant());

        Assert.Equal([byDescription.Id, byName.Id], result.Items.Select(s => s.Id));
    }

    [Fact]
    public async Task Hides_rejected_spots_but_shows_pending_ones()
    {
        await using var db = TestDbContextFactory.Create();
        var marker = Marker();
        var pending = await TestSpotFactory.SeedAsync(db, status: SpotModerationStatus.Pending, name: $"Pending {marker}");
        await TestSpotFactory.SeedAsync(db, status: SpotModerationStatus.Rejected, name: $"Rejected {marker}");

        var result = await SearchAsync(db, marker);

        Assert.Equal([pending.Id], result.Items.Select(s => s.Id));
    }

    [Fact]
    public async Task Filters_by_category()
    {
        await using var db = TestDbContextFactory.Create();
        var marker = Marker();
        var nature = await TestSpotFactory.SeedAsync(db, name: $"Nature {marker}");
        var viewpoint = await TestSpotFactory.SeedAsync(db, name: $"Viewpoint {marker}");
        viewpoint.Category = SpotCategory.Viewpoint;
        await db.SaveChangesAsync();

        var result = await SearchAsync(db, marker, SpotCategory.Nature);

        Assert.Equal([nature.Id], result.Items.Select(s => s.Id));
    }

    [Fact]
    public async Task Ranks_spots_matching_more_of_the_words_first_and_ignores_stop_words()
    {
        await using var db = TestDbContextFactory.Create();
        var first = Marker();
        var second = Marker();
        var both = await TestSpotFactory.SeedAsync(db, name: $"{first} {second}");
        // Created later, so it would come first if ranking fell back to newest-first.
        var one = await TestSpotFactory.SeedAsync(db, name: $"{first} only");

        var result = await SearchAsync(db, $"the {first} of {second}");

        var ours = result.Items.Where(s => s.Id == both.Id || s.Id == one.Id).Select(s => s.Id).ToList();
        Assert.Equal([both.Id, one.Id], ours);
    }

    [Fact]
    public async Task Tolerates_a_typo_through_trigram_similarity()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await TestSpotFactory.SeedAsync(db, name: "Belogradchik rocks");

        var result = await SearchAsync(db, "Belogradcik");

        Assert.Contains(result.Items, s => s.Id == spot.Id);
    }
}
