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

        // All ~ Sofia/Vitosha area except "far", which is Plovdiv (~130km away).
        var veryClose = await SeedSpotAsync(db, $"VeryClose-{Guid.NewGuid():N}", 42.6980, 23.3225);
        var near = await SeedSpotAsync(db, $"Near-{Guid.NewGuid():N}", 42.6588, 23.2745);
        var far = await SeedSpotAsync(db, $"Far-{Guid.NewGuid():N}", 42.1354, 24.7453);

        var handler = new SearchNearbySpots.Handler(db);
        var results = await handler.Handle(new SearchNearbySpots.Query(42.6977, 23.3219, 10), CancellationToken.None);

        var ids = results.Select(r => r.Id).ToList();

        Assert.Contains(veryClose.Id, ids);
        Assert.Contains(near.Id, ids);
        Assert.DoesNotContain(far.Id, ids);
        Assert.True(ids.IndexOf(veryClose.Id) < ids.IndexOf(near.Id));
    }

    // EF Core repeats the ST_Distance expression in both SELECT and ORDER BY (verified via
    // ToQueryString() during development) instead of referencing the projected alias. Confirmed
    // via EXPLAIN ANALYZE against real data that Postgres only evaluates it once per row at
    // execution time regardless (visible as a single "st_distance" in the Sort Key) — so the
    // textual duplication is cosmetic, not a real perf cost. What actually matters, and what this
    // test guards, is that the radius filter uses the GIST index rather than a sequential scan.
    [Fact]
    public async Task Query_uses_the_spatial_index_for_the_radius_filter()
    {
        await using var db = TestDbContextFactory.Create();
        await db.Database.OpenConnectionAsync();

        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            EXPLAIN
            SELECT s."Id"
            FROM "Spots" s
            WHERE ST_DWithin(s."Location", ST_SetSRID(ST_MakePoint(23.3219, 42.6977), 4326)::geography, 10000)
            ORDER BY ST_Distance(s."Location", ST_SetSRID(ST_MakePoint(23.3219, 42.6977), 4326)::geography)
            LIMIT 50
            """;

        var planLines = new List<string>();
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                planLines.Add(reader.GetString(0));
            }
        }

        var plan = string.Join('\n', planLines);
        Assert.Contains("Index Scan", plan);
        Assert.DoesNotContain("Seq Scan", plan);
    }
}
