using Microsoft.AspNetCore.Http;
using NetTopologySuite.Geometries;
using SecretSpots.Domain;
using SecretSpots.Features.Businesses;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Rewards;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Rewards;

public class GetBusinessRewardsTests
{
    private static async Task<Business> SeedBusinessAsync(IAppDbContext db)
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
        await db.SaveChangesAsync();
        return business;
    }

    private static async Task SeedRewardAsync(IAppDbContext db, Guid businessId, string title)
    {
        db.Rewards.Add(new Reward
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Title = title,
            Description = "test",
            CrystalCost = 10,
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Returns_rewards_for_the_business()
    {
        await using var db = TestDbContextFactory.Create();
        var business = await SeedBusinessAsync(db);
        await SeedRewardAsync(db, business.Id, "Reward A");
        await SeedRewardAsync(db, business.Id, "Reward B");

        var otherBusiness = await SeedBusinessAsync(db);
        await SeedRewardAsync(db, otherBusiness.Id, "Other business reward");

        var handler = new GetBusinessRewards.Handler(db, TestLocalizerFactory.Create());
        var result = await handler.Handle(new GetBusinessRewards.Query(business.Id, 1, 20), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.All(result.Value.Items, r => Assert.Equal(business.Id, r.BusinessId));
    }

    [Fact]
    public async Task Pagination_slices_correctly_and_reports_total_count()
    {
        await using var db = TestDbContextFactory.Create();
        var business = await SeedBusinessAsync(db);
        await SeedRewardAsync(db, business.Id, "Reward A");
        await SeedRewardAsync(db, business.Id, "Reward B");
        await SeedRewardAsync(db, business.Id, "Reward C");

        var handler = new GetBusinessRewards.Handler(db, TestLocalizerFactory.Create());
        var result = await handler.Handle(new GetBusinessRewards.Query(business.Id, 1, 2), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalCount);
        Assert.Equal(2, result.Value.Items.Count);
    }

    [Fact]
    public async Task Nonexistent_business_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetBusinessRewards.Handler(db, TestLocalizerFactory.Create());

        var result = await handler.Handle(new GetBusinessRewards.Query(Guid.NewGuid(), 1, 20), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BusinessesMessageKeys.NotFound, result.Error.Code);
        Assert.Equal(StatusCodes.Status404NotFound, result.Error.StatusCode);
    }
}
