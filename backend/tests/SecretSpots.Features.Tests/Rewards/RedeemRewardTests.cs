using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NetTopologySuite.Geometries;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Rewards;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Rewards;

public class RedeemRewardTests
{
    private static async Task<Reward> SeedRewardAsync(IAppDbContext db, int crystalCost = 10)
    {
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = $"Business-{Guid.NewGuid():N}",
            Description = "test",
            Location = new Point(23.3219, 42.6977) { SRID = 4326 },
            OwnerUserId = Guid.NewGuid(),
        };
        db.Businesses.Add(business);

        var reward = new Reward
        {
            Id = Guid.NewGuid(),
            BusinessId = business.Id,
            Title = "Free coffee",
            Description = "test",
            CrystalCost = crystalCost,
        };
        db.Rewards.Add(reward);

        await db.SaveChangesAsync();

        return reward;
    }

    private static RedeemReward.Handler CreateHandler(IAppDbContext db, Guid userId) =>
        new(db, new FakeUserContext(userId), TestLocalizerFactory.Create(), NullLogger<RedeemReward.Handler>.Instance);

    [Fact]
    public async Task Successful_redemption_deducts_the_balance_and_persists_a_redemption_record()
    {
        await using var db = TestDbContextFactory.Create();
        var reward = await SeedRewardAsync(db, crystalCost: 15);
        var user = await TestUserFactory.SeedAsync(db, $"redeem-{Guid.NewGuid():N}@example.com", "Str0ng!Passw0rd1");
        user.CrystalBalance = 20;
        await db.SaveChangesAsync();

        var handler = CreateHandler(db, user.Id);
        var result = await handler.Handle(new RedeemReward.Command(reward.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(15, result.Value.CrystalsSpent);
        Assert.Equal(5, result.Value.NewCrystalBalance);

        var savedUser = await db.Users.SingleAsync(u => u.Id == user.Id);
        Assert.Equal(5, savedUser.CrystalBalance);

        var redemption = await db.RewardRedemptions.SingleAsync(r => r.RewardId == reward.Id);
        Assert.Equal(user.Id, redemption.UserId);
        Assert.Equal(reward.BusinessId, redemption.BusinessId);
        Assert.Equal(15, redemption.CrystalsSpent);
    }

    [Fact]
    public async Task Nonexistent_reward_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();
        var user = await TestUserFactory.SeedAsync(db, $"redeem-{Guid.NewGuid():N}@example.com", "Str0ng!Passw0rd1");

        var handler = CreateHandler(db, user.Id);
        var result = await handler.Handle(new RedeemReward.Command(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(RewardsMessageKeys.NotFound, result.Error.Code);
        Assert.Equal(StatusCodes.Status404NotFound, result.Error.StatusCode);
    }

    [Fact]
    public async Task Insufficient_balance_is_rejected_and_nothing_is_persisted()
    {
        await using var db = TestDbContextFactory.Create();
        var reward = await SeedRewardAsync(db, crystalCost: 50);
        var user = await TestUserFactory.SeedAsync(db, $"redeem-{Guid.NewGuid():N}@example.com", "Str0ng!Passw0rd1");
        user.CrystalBalance = 10;
        await db.SaveChangesAsync();

        var handler = CreateHandler(db, user.Id);
        var result = await handler.Handle(new RedeemReward.Command(reward.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(RewardsMessageKeys.InsufficientBalance, result.Error.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, result.Error.StatusCode);

        var savedUser = await db.Users.SingleAsync(u => u.Id == user.Id);
        Assert.Equal(10, savedUser.CrystalBalance);
        Assert.False(await db.RewardRedemptions.AnyAsync(r => r.RewardId == reward.Id));
    }
}
