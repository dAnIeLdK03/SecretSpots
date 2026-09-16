using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NetTopologySuite.Geometries;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Rewards;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Rewards;

public class SetRewardActiveTests
{
    private static async Task<(Business Business, Reward Reward)> SeedAsync(IAppDbContext db, Guid ownerId)
    {
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = $"Business-{Guid.NewGuid():N}",
            Description = "test",
            Location = new Point(23.3219, 42.6977) { SRID = 4326 },
            OwnerUserId = ownerId,
        };
        db.Businesses.Add(business);

        var reward = new Reward
        {
            Id = Guid.NewGuid(),
            BusinessId = business.Id,
            Title = "Title",
            Description = "Description",
            CrystalCost = 10,
        };
        db.Rewards.Add(reward);

        await db.SaveChangesAsync();

        return (business, reward);
    }

    private static SetRewardActive.Handler CreateHandler(IAppDbContext db, Guid userId) =>
        new(db, new FakeUserContext(userId), TestLocalizerFactory.Create(), NullLogger<SetRewardActive.Handler>.Instance);

    [Fact]
    public async Task Owner_can_pause_and_resume_their_reward()
    {
        await using var db = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var (_, reward) = await SeedAsync(db, ownerId);

        var handler = CreateHandler(db, ownerId);

        var paused = await handler.Handle(new SetRewardActive.Command(reward.Id, false), CancellationToken.None);
        Assert.True(paused.IsSuccess);
        Assert.False(paused.Value.IsActive);
        Assert.False((await db.Rewards.SingleAsync(r => r.Id == reward.Id)).IsActive);

        var resumed = await handler.Handle(new SetRewardActive.Command(reward.Id, true), CancellationToken.None);
        Assert.True(resumed.IsSuccess);
        Assert.True(resumed.Value.IsActive);
    }

    [Fact]
    public async Task Non_owner_cannot_change_active_state()
    {
        await using var db = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var (_, reward) = await SeedAsync(db, ownerId);

        var handler = CreateHandler(db, Guid.NewGuid());
        var result = await handler.Handle(new SetRewardActive.Command(reward.Id, false), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(RewardsMessageKeys.NotYourBusiness, result.Error.Code);
        Assert.Equal(StatusCodes.Status403Forbidden, result.Error.StatusCode);
        Assert.True((await db.Rewards.SingleAsync(r => r.Id == reward.Id)).IsActive);
    }

    [Fact]
    public async Task Nonexistent_reward_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = CreateHandler(db, Guid.NewGuid());

        var result = await handler.Handle(new SetRewardActive.Command(Guid.NewGuid(), false), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(RewardsMessageKeys.NotFound, result.Error.Code);
        Assert.Equal(StatusCodes.Status404NotFound, result.Error.StatusCode);
    }
}
