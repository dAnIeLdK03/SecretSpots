using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Spots;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Spots;

public class SearchNearbySpotsValidatorTests
{
    private readonly SearchNearbySpots.Validator _validator = new(TestLocalizerFactory.Create());

    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    public void Latitude_out_of_range(double latitude)
    {
        var result = _validator.TestValidate(new SearchNearbySpots.Query(latitude, 23.3219, 10));
        result.ShouldHaveValidationErrorFor(q => q.Latitude);
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    public void Longitude_out_of_range(double longitude)
    {
        var result = _validator.TestValidate(new SearchNearbySpots.Query(42.6977, longitude, 10));
        result.ShouldHaveValidationErrorFor(q => q.Longitude);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100)]
    [InlineData(101)]
    public void RadiusKm_out_of_range(double radiusKm)
    {
        var result = _validator.TestValidate(new SearchNearbySpots.Query(42.6977, 23.3219, radiusKm));
        result.ShouldHaveValidationErrorFor(q => q.RadiusKm);
    }

    [Fact]
    public void Valid_query_has_no_errors()
    {
        var result = _validator.TestValidate(new SearchNearbySpots.Query(42.6977, 23.3219, 10));
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class SearchNearbySpotsHandlerTests
{
    private static async Task<Spot> SeedSpotAsync(IAppDbContext db, string name, double latitude, double longitude)
    {
        var spot = new Spot
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "test",
            Category = SpotCategory.Nature,
            PhotoUrls = ["https://example.com/photo.jpg"],
            Location = new Point(longitude, latitude) { SRID = 4326 },
            CreatedByUserId = Guid.NewGuid(),
        };

        db.Spots.Add(spot);
        await db.SaveChangesAsync();

        return spot;
    }

    [Fact]
    public async Task Returns_spots_within_radius_sorted_by_distance_and_excludes_far_ones()
    {
        await using var db = TestDbContextFactory.Create();

        // Centered on a random point instead of the literal (42.6977, 23.3219) "Sofia center"
        // reused by dozens of other test files that seed spots into this same shared, never-
        // cleaned Postgres test database. Landing on that exact point meant this test's own
        // "near"/"veryClose" spots would eventually get pushed past the handler's MaxResults=50
        // cutoff by accumulated rows from repeated suite runs, failing intermittently with
        // "not found" for a random id. The offsets below are unchanged from the original
        // Sofia/Vitosha/Plovdiv layout (~50m / ~5km / ~130km from center) — only the center moves.
        var centerLat = Random.Shared.NextDouble() * 120 - 60;
        var centerLng = Random.Shared.NextDouble() * 300 - 150;

        var veryClose = await SeedSpotAsync(db, $"VeryClose-{Guid.NewGuid():N}", centerLat + 0.0003, centerLng + 0.0006);
        var near = await SeedSpotAsync(db, $"Near-{Guid.NewGuid():N}", centerLat - 0.0389, centerLng - 0.0474);
        var far = await SeedSpotAsync(db, $"Far-{Guid.NewGuid():N}", centerLat - 0.5623, centerLng + 1.4234);

        var handler = new SearchNearbySpots.Handler(db);
        var results = await handler.Handle(new SearchNearbySpots.Query(centerLat, centerLng, 10), CancellationToken.None);

        var ids = results.Select(r => r.Id).ToList();

        Assert.Contains(veryClose.Id, ids);
        Assert.Contains(near.Id, ids);
        Assert.DoesNotContain(far.Id, ids);
        Assert.True(ids.IndexOf(veryClose.Id) < ids.IndexOf(near.Id));
    }
}
