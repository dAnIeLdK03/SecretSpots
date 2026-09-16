using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NetTopologySuite.Geometries;
using SecretSpots.Domain;
using SecretSpots.Features.Businesses;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Businesses;

public class SetBusinessActiveTests
{
    private static async Task<Business> SeedAsync(IAppDbContext db, Guid ownerId)
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
        await db.SaveChangesAsync();
        return business;
    }

    private static SetBusinessActive.Handler CreateHandler(IAppDbContext db, Guid userId) =>
        new(db, new FakeUserContext(userId), TestLocalizerFactory.Create(), NullLogger<SetBusinessActive.Handler>.Instance);

    [Fact]
    public async Task Owner_can_pause_and_resume_their_business()
    {
        await using var db = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var business = await SeedAsync(db, ownerId);

        var handler = CreateHandler(db, ownerId);

        var paused = await handler.Handle(new SetBusinessActive.Command(business.Id, false), CancellationToken.None);
        Assert.True(paused.IsSuccess);
        Assert.False(paused.Value.IsActive);
        Assert.False((await db.Businesses.SingleAsync(b => b.Id == business.Id)).IsActive);

        var resumed = await handler.Handle(new SetBusinessActive.Command(business.Id, true), CancellationToken.None);
        Assert.True(resumed.IsSuccess);
        Assert.True(resumed.Value.IsActive);
    }

    [Fact]
    public async Task Non_owner_cannot_change_active_state()
    {
        await using var db = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var business = await SeedAsync(db, ownerId);

        var handler = CreateHandler(db, Guid.NewGuid());
        var result = await handler.Handle(new SetBusinessActive.Command(business.Id, false), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BusinessesMessageKeys.NotYourBusiness, result.Error.Code);
        Assert.Equal(StatusCodes.Status403Forbidden, result.Error.StatusCode);
        Assert.True((await db.Businesses.SingleAsync(b => b.Id == business.Id)).IsActive);
    }

    [Fact]
    public async Task Nonexistent_business_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = CreateHandler(db, Guid.NewGuid());

        var result = await handler.Handle(new SetBusinessActive.Command(Guid.NewGuid(), false), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BusinessesMessageKeys.NotFound, result.Error.Code);
        Assert.Equal(StatusCodes.Status404NotFound, result.Error.StatusCode);
    }
}
