using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NetTopologySuite.Geometries;
using SecretSpots.Domain;
using SecretSpots.Features.Businesses;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Businesses;

public class DeleteBusinessTests
{
    private static async Task<(Business Business, Reward Reward)> SeedAsync(IAppDbContext db)
    {
        var business = new Business
        {
            Id = Guid.NewGuid(), Name = "Cafe", Description = "Coffee", OwnerUserId = Guid.NewGuid(),
            Location = new Point(23.3219, 42.6977) { SRID = 4326 },
        };
        var reward = new Reward { Id = Guid.NewGuid(), BusinessId = business.Id, Title = "Free coffee", Description = "One", CrystalCost = 10 };
        db.Businesses.Add(business);
        db.Rewards.Add(reward);
        await db.SaveChangesAsync();
        return (business, reward);
    }

    private static DeleteBusiness.Handler CreateHandler(IAppDbContext db, Guid userId) =>
        new(db, new FakeUserContext(userId), TestLocalizerFactory.Create(), NullLogger<DeleteBusiness.Handler>.Instance);

    [Fact]
    public async Task Owner_deletes_the_business_together_with_its_rewards()
    {
        await using var db = TestDbContextFactory.Create();
        var (business, reward) = await SeedAsync(db);

        var result = await CreateHandler(db, business.OwnerUserId).Handle(new DeleteBusiness.Command(business.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        db.ChangeTracker.Clear();
        Assert.False(await db.Businesses.AnyAsync(b => b.Id == business.Id));
        Assert.False(await db.Rewards.AnyAsync(r => r.Id == reward.Id));
    }

    [Fact]
    public async Task Only_the_owner_can_delete_and_a_missing_business_is_not_found()
    {
        await using var db = TestDbContextFactory.Create();
        var (business, _) = await SeedAsync(db);

        var stranger = await CreateHandler(db, Guid.NewGuid()).Handle(new DeleteBusiness.Command(business.Id), CancellationToken.None);
        var missing = await CreateHandler(db, business.OwnerUserId).Handle(new DeleteBusiness.Command(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(BusinessesMessageKeys.NotYourBusiness, stranger.Error.Code);
        Assert.Equal(BusinessesMessageKeys.NotFound, missing.Error.Code);
        Assert.True(await db.Businesses.AnyAsync(b => b.Id == business.Id));
    }
}
