using SecretSpots.Domain;
using SecretSpots.Features.Ratings;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Ratings;

public class GetMyRatingTests
{
    [Fact]
    public async Task Returns_the_callers_own_rating_or_null()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await TestSpotFactory.SeedAsync(db);
        var raterId = Guid.NewGuid();
        db.Ratings.Add(new Rating { Id = Guid.NewGuid(), SpotId = spot.Id, UserId = raterId, Value = 4 });
        db.Ratings.Add(new Rating { Id = Guid.NewGuid(), SpotId = spot.Id, UserId = Guid.NewGuid(), Value = 1 });
        await db.SaveChangesAsync();

        var mine = await new GetMyRating.Handler(db, new FakeUserContext(raterId)).Handle(new GetMyRating.Query(spot.Id), CancellationToken.None);
        var none = await new GetMyRating.Handler(db, new FakeUserContext(Guid.NewGuid())).Handle(new GetMyRating.Query(spot.Id), CancellationToken.None);

        Assert.Equal(4, mine);
        Assert.Null(none);
    }
}
