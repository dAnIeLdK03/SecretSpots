using NetTopologySuite.Geometries;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;

namespace SecretSpots.Features.Tests.TestSupport;

internal static class TestSpotFactory
{
    public static async Task<Spot> SeedAsync(
        IAppDbContext db,
        Guid? createdByUserId = null,
        SpotModerationStatus status = SpotModerationStatus.Approved,
        string? name = null,
        List<string>? photoUrls = null,
        double longitude = 23.3219,
        double latitude = 42.6977)
    {
        var spot = new Spot
        {
            Id = Guid.NewGuid(),
            Name = name ?? $"Test spot {Guid.NewGuid():N}",
            Description = "A quiet place for tests",
            Category = SpotCategory.Nature,
            Location = new Point(longitude, latitude) { SRID = 4326 },
            PhotoUrls = photoUrls ?? ["https://example.com/photo.jpg"],
            CreatedByUserId = createdByUserId ?? Guid.NewGuid(),
            ModerationStatus = status,
        };

        db.Spots.Add(spot);
        await db.SaveChangesAsync();

        return spot;
    }
}
