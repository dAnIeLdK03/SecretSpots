using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NetTopologySuite.Geometries;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Rewards;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Rewards;

public class DeleteRewardTests
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

    private static DeleteReward.Handler CreateHandler(IAppDbContext db, Guid userId) =>
        new(db, new FakeUserContext(userId), TestLocalizerFactory.Create(), NullLogger<DeleteReward.Handler>.Instance);

    [Fact]
    public async Task Owner_can_delete_their_reward()
    {
        await using var db = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var (_, reward) = await SeedAsync(db, ownerId);

        var handler = CreateHandler(db, ownerId);
        var result = await handler.Handle(new DeleteReward.Command(reward.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(await db.Rewards.AnyAsync(r => r.Id == reward.Id));
    }

    [Fact]
    public async Task Deleting_a_reward_leaves_its_redemption_history_intact()
    {
        await using var db = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var (business, reward) = await SeedAsync(db, ownerId);

        var redemption = new RewardRedemption
        {
            Id = Guid.NewGuid(),
            RewardId = reward.Id,
            BusinessId = business.Id,
            UserId = Guid.NewGuid(),
            CrystalsSpent = reward.CrystalCost,
            RewardTitle = reward.Title,
            BusinessName = business.Name,
        };
        db.RewardRedemptions.Add(redemption);
        await db.SaveChangesAsync();

        var handler = CreateHandler(db, ownerId);
        var result = await handler.Handle(new DeleteReward.Command(reward.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);

        var savedRedemption = await db.RewardRedemptions.SingleAsync(r => r.Id == redemption.Id);
        Assert.Equal(reward.Title, savedRedemption.RewardTitle);
        Assert.Equal(business.Name, savedRedemption.BusinessName);
        Assert.Equal(reward.CrystalCost, savedRedemption.CrystalsSpent);
    }

    [Fact]
    public async Task Nonexistent_reward_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = CreateHandler(db, Guid.NewGuid());

        var result = await handler.Handle(new DeleteReward.Command(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(RewardsMessageKeys.NotFound, result.Error.Code);
        Assert.Equal(StatusCodes.Status404NotFound, result.Error.StatusCode);
    }

    [Fact]
    public async Task Non_owner_cannot_delete_the_reward()
    {
        await using var db = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var (_, reward) = await SeedAsync(db, ownerId);

        var handler = CreateHandler(db, Guid.NewGuid());
        var result = await handler.Handle(new DeleteReward.Command(reward.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(RewardsMessageKeys.NotYourBusiness, result.Error.Code);
        Assert.Equal(StatusCodes.Status403Forbidden, result.Error.StatusCode);

        Assert.True(await db.Rewards.AnyAsync(r => r.Id == reward.Id));
    }
}
