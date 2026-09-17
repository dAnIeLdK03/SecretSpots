using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NetTopologySuite.Geometries;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Ratings;
using SecretSpots.Features.Tests.TestSupport;
using WebPush;

namespace SecretSpots.Features.Tests.Ratings;

public class RateSpotTests
{
    private static async Task<Spot> SeedSpotAsync(IAppDbContext db, Guid createdByUserId)
    {
        var spot = new Spot
        {
            Id = Guid.NewGuid(),
            Name = $"Spot-{Guid.NewGuid():N}",
            Description = "test",
            Category = SpotCategory.Nature,
            PhotoUrls = ["https://example.com/photo.jpg"],
            Location = new Point(23.3219, 42.6977) { SRID = 4326 },
            CreatedByUserId = createdByUserId,
        };

        db.Spots.Add(spot);
        await db.SaveChangesAsync();

        return spot;
    }

    private static RateSpot.Handler CreateHandler(IAppDbContext db, Guid userId) =>
        new(db, new FakeUserContext(userId), new WebPushClient(), TestOptionsFactory.WebPush(),
            TestLocalizerFactory.Create(), NullLogger<RateSpot.Handler>.Instance);

    [Fact]
    public async Task Creator_cannot_rate_their_own_spot()
    {
        await using var db = TestDbContextFactory.Create();
        var creatorId = Guid.NewGuid();
        var spot = await SeedSpotAsync(db, creatorId);

        var handler = CreateHandler(db, creatorId);
        var result = await handler.Handle(new RateSpot.Command(spot.Id, 5), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(RatingsMessageKeys.CannotRateOwnSpot, result.Error.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, result.Error.StatusCode);
        Assert.False(await db.Ratings.AnyAsync(r => r.SpotId == spot.Id));

        var savedSpot = await db.Spots.SingleAsync(s => s.Id == spot.Id);
        Assert.Equal(0, savedSpot.RatingsCount);
    }

    [Fact]
    public async Task Other_user_can_rate_the_spot_and_average_is_recomputed()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await SeedSpotAsync(db, Guid.NewGuid());
        var raterId = Guid.NewGuid();

        var handler = CreateHandler(db, raterId);
        var result = await handler.Handle(new RateSpot.Command(spot.Id, 4), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value.Value);
        Assert.Equal(4, result.Value.AverageRating);
        Assert.Equal(1, result.Value.RatingsCount);

        var rating = await db.Ratings.SingleAsync(r => r.SpotId == spot.Id && r.UserId == raterId);
        Assert.Equal(4, rating.Value);
    }
}
