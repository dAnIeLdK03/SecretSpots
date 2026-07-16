using Microsoft.AspNetCore.Http;
using NetTopologySuite.Geometries;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Spots;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Spots;

public class GetSpotTests
{
    private static async Task<Spot> SeedAsync(IAppDbContext db)
    {
        var spot = new Spot
        {
            Id = Guid.NewGuid(),
            Name = $"Spot-{Guid.NewGuid():N}",
            Description = "test",
            Category = SpotCategory.Nature,
            PhotoUrl = "https://example.com/photo.jpg",
            Location = new Point(23.3219, 42.6977) { SRID = 4326 },
            CreatedByUserId = Guid.NewGuid(),
        };

        db.Spots.Add(spot);
        await db.SaveChangesAsync();

        return spot;
    }

    [Fact]
    public async Task Returns_the_spot_when_it_exists()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await SeedAsync(db);

        var handler = new GetSpot.Handler(db, TestLocalizerFactory.Create());
        var result = await handler.Handle(new GetSpot.Query(spot.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(spot.Name, result.Value.Name);
    }

    [Fact]
    public async Task Nonexistent_spot_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetSpot.Handler(db, TestLocalizerFactory.Create());

        var result = await handler.Handle(new GetSpot.Query(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SpotsMessageKeys.NotFound, result.Error.Code);
        Assert.Equal(StatusCodes.Status404NotFound, result.Error.StatusCode);
    }
}
