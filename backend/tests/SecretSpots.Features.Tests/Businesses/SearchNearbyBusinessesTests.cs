using NetTopologySuite.Geometries;
using SecretSpots.Domain;
using SecretSpots.Features.Businesses;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Businesses;

public class SearchNearbyBusinessesTests
{
    private static async Task<Business> SeedAsync(
        IAppDbContext db, double latitude, double longitude, bool isActive = true)
    {
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = $"Business-{Guid.NewGuid():N}",
            Description = "test",
            Location = new Point(longitude, latitude) { SRID = 4326 },
            OwnerUserId = Guid.NewGuid(),
            IsActive = isActive,
        };
        db.Businesses.Add(business);
        await db.SaveChangesAsync();
        return business;
    }

    [Fact]
    public async Task Paused_business_is_excluded_from_nearby_results()
    {
        await using var db = TestDbContextFactory.Create();

        // Same reasoning as the equivalent SearchNearbySpots tests: a random center avoids
        // collisions with rows seeded by other test files in this shared, never-cleaned DB.
        var centerLat = Random.Shared.NextDouble() * 120 - 60;
        var centerLng = Random.Shared.NextDouble() * 300 - 150;

        var active = await SeedAsync(db, centerLat + 0.0003, centerLng + 0.0006, isActive: true);
        var paused = await SeedAsync(db, centerLat + 0.0004, centerLng + 0.0007, isActive: false);

        var handler = new SearchNearbyBusinesses.Handler(db);
        var result = await handler.Handle(new SearchNearbyBusinesses.Query(centerLat, centerLng, 10), CancellationToken.None);

        var ids = result.Items.Select(b => b.Id).ToList();

        Assert.Contains(active.Id, ids);
        Assert.DoesNotContain(paused.Id, ids);
    }
}
