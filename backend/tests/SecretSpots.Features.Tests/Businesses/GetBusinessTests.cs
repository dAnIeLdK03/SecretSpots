using Microsoft.AspNetCore.Http;
using NetTopologySuite.Geometries;
using SecretSpots.Domain;
using SecretSpots.Features.Businesses;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Businesses;

public class GetBusinessTests
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

    [Fact]
    public async Task Returns_the_business_when_it_exists()
    {
        await using var db = TestDbContextFactory.Create();
        var business = await SeedAsync(db, Guid.NewGuid());

        var handler = new GetBusiness.Handler(db, TestLocalizerFactory.Create());
        var result = await handler.Handle(new GetBusiness.Query(business.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(business.Name, result.Value.Name);
    }

    [Fact]
    public async Task Nonexistent_business_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetBusiness.Handler(db, TestLocalizerFactory.Create());

        var result = await handler.Handle(new GetBusiness.Query(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BusinessesMessageKeys.NotFound, result.Error.Code);
        Assert.Equal(StatusCodes.Status404NotFound, result.Error.StatusCode);
    }
}
