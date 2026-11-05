using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SecretSpots.Domain;
using SecretSpots.Features.Assistant;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Assistant;

// The tools run through the real ISender, handlers and validators against the test database, so
// these also cover what the assistant can and can't see.
public class AssistantToolsTests
{
    private static ServiceProvider CreateServices(IAppDbContext db)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization();
        services.AddMediator(typeof(AssistantTools).Assembly);
        services.AddValidatorsFromAssembly(typeof(AssistantTools).Assembly);
        services.AddSingleton(db);
        services.AddSingleton<IUserContext>(new FakeUserContext(Guid.NewGuid()));
        services.AddScoped<AssistantTools>();
        return services.BuildServiceProvider();
    }

    private static AssistantTools Tools(ServiceProvider services) => services.GetRequiredService<AssistantTools>();

    private static string Marker() => "a" + Guid.NewGuid().ToString("N")[..12];

    private static async Task<Spot> SeedAsync(
        IAppDbContext db, string name, SpotModerationStatus status = SpotModerationStatus.Approved,
        double rating = 0, int ratingsCount = 0, double longitude = 23.3219, double latitude = 42.6977, string? description = null)
    {
        var spot = await TestSpotFactory.SeedAsync(db, status: status, name: name, longitude: longitude, latitude: latitude);
        spot.AverageRating = rating;
        spot.RatingsCount = ratingsCount;
        if (description is not null)
        {
            spot.Description = description;
        }

        await db.SaveChangesAsync();
        return spot;
    }

    [Fact]
    public async Task Search_returns_only_approved_spots()
    {
        await using var db = TestDbContextFactory.Create();
        var marker = Marker();
        var approved = await SeedAsync(db, $"Approved {marker}", rating: 4.25, ratingsCount: 4);
        await SeedAsync(db, $"Pending {marker}", SpotModerationStatus.Pending);
        await SeedAsync(db, $"Rejected {marker}", SpotModerationStatus.Rejected);
        var deleted = await SeedAsync(db, $"Deleted {marker}");
        deleted.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        await using var services = CreateServices(db);

        var result = await Tools(services).SearchSpotsAsync(marker);

        var spot = Assert.Single(result.Spots);
        Assert.Equal((approved.Id, 4.3, 4, "Nature"), (spot.Id, spot.AverageRating, spot.RatingsCount, spot.Category));
        Assert.Null(spot.DistanceKm);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task Long_descriptions_are_cut_to_the_limit()
    {
        await using var db = TestDbContextFactory.Create();
        var marker = Marker();
        await SeedAsync(db, $"Long {marker}", description: new string('x', 1000));
        await SeedAsync(db, $"Short {marker}", description: "Short and sweet.");
        await using var services = CreateServices(db);

        var spots = (await Tools(services).SearchSpotsAsync(marker)).Spots.ToDictionary(s => s.Name);

        var longOne = spots[$"Long {marker}"].Description;
        Assert.Equal(AssistantTools.MaxDescriptionLength, longOne.Length);
        Assert.EndsWith("…", longOne);
        Assert.Equal("Short and sweet.", spots[$"Short {marker}"].Description);
    }

    [Theory]
    [InlineData("waterfall")]
    [InlineData("WATERFALL")]
    public async Task Category_names_are_matched_case_insensitively(string category)
    {
        await using var db = TestDbContextFactory.Create();
        var marker = Marker();
        var waterfall = await SeedAsync(db, $"Falls {marker}");
        waterfall.Category = SpotCategory.Waterfall;
        await SeedAsync(db, $"Forest {marker}");
        await db.SaveChangesAsync();
        await using var services = CreateServices(db);

        var result = await Tools(services).SearchSpotsAsync(marker, category);

        Assert.Equal([waterfall.Id], result.Spots.Select(s => s.Id));
    }

    [Theory]
    [InlineData("Volcano")]
    [InlineData("3")]
    public async Task An_unknown_category_comes_back_as_an_error_listing_the_valid_ones(string category)
    {
        await using var db = TestDbContextFactory.Create();
        await using var services = CreateServices(db);

        var result = await Tools(services).SearchSpotsAsync("lake", category);

        Assert.Empty(result.Spots);
        Assert.Contains("Waterfall", result.Error);
    }

    [Fact]
    public async Task Nearby_search_keeps_only_approved_spots_above_the_minimum_rating_nearest_first()
    {
        await using var db = TestDbContextFactory.Create();
        // Random centre, like SearchNearbySpotsHandlerTests: the shared DB fills up around any fixed point.
        var lat = Random.Shared.NextDouble() * 120 - 60;
        var lng = Random.Shared.NextDouble() * 300 - 150;
        var nearGood = await SeedAsync(db, "Near good", rating: 4.6, ratingsCount: 5, longitude: lng + 0.01, latitude: lat);
        var farGood = await SeedAsync(db, "Far good", rating: 4.1, ratingsCount: 2, longitude: lng + 0.2, latitude: lat);
        var mediocre = await SeedAsync(db, "Mediocre", rating: 3.2, ratingsCount: 9, longitude: lng + 0.02, latitude: lat);
        var unrated = await SeedAsync(db, "Unrated", longitude: lng + 0.03, latitude: lat);
        await SeedAsync(db, "Rejected but great", SpotModerationStatus.Rejected, 5, 3, lng + 0.005, lat);
        await using var services = CreateServices(db);
        var tools = Tools(services);

        var anyRating = await tools.FindSpotsNearAsync(lat, lng, 50);
        var atLeastFour = await tools.FindSpotsNearAsync(lat, lng, 50, minRating: 4);

        Assert.Equal([nearGood.Id, mediocre.Id, unrated.Id, farGood.Id], anyRating.Spots.Select(s => s.Id));
        Assert.Equal([nearGood.Id, farGood.Id], atLeastFour.Spots.Select(s => s.Id));
        Assert.All(atLeastFour.Spots, s => Assert.Equal(Math.Round(s.DistanceKm!.Value, 1, MidpointRounding.AwayFromZero), s.DistanceKm));
    }

    [Fact]
    public async Task Bad_nearby_input_is_clamped_or_reported_instead_of_throwing()
    {
        await using var db = TestDbContextFactory.Create();
        await using var services = CreateServices(db);
        var tools = Tools(services);
        // Away from the fixed points other tests seed around (see the nearby test above).
        var lat = Random.Shared.NextDouble() * 120 - 60;
        var lng = Random.Shared.NextDouble() * 300 - 150;

        var hugeRadius = await tools.FindSpotsNearAsync(lat, lng, 5000);
        var badLatitude = await tools.FindSpotsNearAsync(200, lng, 10);
        var badRating = await tools.FindSpotsNearAsync(lat, lng, 10, minRating: 9);

        Assert.Null(hugeRadius.Error);
        Assert.NotNull(badLatitude.Error);
        Assert.NotNull(badRating.Error);
    }

    [Fact]
    public async Task Get_spot_returns_approved_spots_with_coordinates_and_hides_the_rest()
    {
        await using var db = TestDbContextFactory.Create();
        var approved = await SeedAsync(db, $"Visible {Marker()}", longitude: 24.75, latitude: 42.14);
        var pending = await SeedAsync(db, $"Unreviewed {Marker()}", SpotModerationStatus.Pending);
        await using var services = CreateServices(db);
        var tools = Tools(services);

        var found = await tools.GetSpotAsync(approved.Id);
        var hidden = await tools.GetSpotAsync(pending.Id);
        var missing = await tools.GetSpotAsync(Guid.NewGuid());

        Assert.Equal((approved.Id, 42.14, 24.75), (found.Spot!.Id, found.Latitude!.Value, found.Longitude!.Value));
        Assert.Null(hidden.Spot);
        Assert.NotNull(hidden.Error);
        Assert.Equal(hidden.Error, missing.Error);
    }

    [Fact]
    public async Task Comments_are_the_latest_few_cut_to_length_and_only_for_visible_spots()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await SeedAsync(db, $"Commented {Marker()}");
        var rejected = await SeedAsync(db, $"Rejected {Marker()}", SpotModerationStatus.Rejected);
        var author = await TestUserFactory.SeedAsync(db, displayName: "Hiker Hristo");
        for (var i = 0; i < AssistantTools.MaxComments + 2; i++)
        {
            var comment = await TestCommentFactory.SeedAsync(db, spot.Id, author.Id, text: $"Visit {i} " + new string('y', i == 0 ? 0 : 400));
            comment.CreatedAt = DateTimeOffset.UtcNow.AddMinutes(i);
        }

        await TestCommentFactory.SeedAsync(db, rejected.Id, author.Id, text: "Hidden");
        await db.SaveChangesAsync();
        await using var services = CreateServices(db);
        var tools = Tools(services);

        var result = await tools.GetSpotCommentsAsync(spot.Id);
        var onRejected = await tools.GetSpotCommentsAsync(rejected.Id);

        Assert.Equal(AssistantTools.MaxComments, result.Comments.Count);
        Assert.Equal(AssistantTools.MaxComments + 2, result.TotalCount);
        Assert.StartsWith($"Visit {AssistantTools.MaxComments + 1} ", result.Comments[0].Text);
        Assert.All(result.Comments, c =>
        {
            Assert.Equal("Hiker Hristo", c.AuthorDisplayName);
            Assert.True(c.Text.Length <= AssistantTools.MaxCommentLength);
        });
        Assert.Empty(onRejected.Comments);
        Assert.NotNull(onRejected.Error);
    }

    [Theory]
    [InlineData("Пловдив", "Пловдив")]
    [InlineData("гр. Пловдив", "Пловдив")]
    [InlineData("град Пловдив", "Пловдив")]
    [InlineData("  PLOVDIV ", "Пловдив")]
    [InlineData("Velik-Tarnovo", null)]
    [InlineData("Veliko-Tarnovo", "Велико Търново")]
    [InlineData("велико   търново", "Велико Търново")]
    [InlineData("Търново", "Велико Търново")]
    [InlineData("Sofiya", "София")]
    [InlineData("Gotham", null)]
    [InlineData("", null)]
    public async Task Locate_place_understands_common_spellings(string name, string? expected)
    {
        await using var db = TestDbContextFactory.Create();
        await using var services = CreateServices(db);

        var result = await Tools(services).LocatePlaceAsync(name);

        Assert.Equal(expected, result.Name);
        if (expected is null)
        {
            Assert.NotNull(result.Error);
            Assert.Null(result.Latitude);
        }
        else
        {
            Assert.InRange(result.Latitude!.Value, 41, 45);
            Assert.InRange(result.Longitude!.Value, 22, 29);
        }
    }
}
