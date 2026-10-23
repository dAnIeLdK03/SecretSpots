using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NetTopologySuite.Geometries;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Spots;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Spots;

public class SubmitSpotDeletionTests
{
    private static async Task<Spot> SeedAsync(IAppDbContext db, Guid createdByUserId)
    {
        var spot = new Spot
        {
            Id = Guid.NewGuid(),
            Name = "Name",
            Description = "Description",
            Category = SpotCategory.Nature,
            PhotoUrls = ["https://example.com/a.jpg"],
            Location = new Point(23.3219, 42.6977) { SRID = 4326 },
            CreatedByUserId = createdByUserId,
        };
        db.Spots.Add(spot);

        await db.SaveChangesAsync();

        return spot;
    }

    private static SubmitSpotDeletion.Handler CreateHandler(IAppDbContext db, Guid userId) =>
        new(db, new FakeUserContext(userId), new FakePhotoStorage(), TestLocalizerFactory.Create(), NullLogger<SubmitSpotDeletion.Handler>.Instance);

    [Fact]
    public async Task Creator_submits_a_pending_deletion_and_the_spot_stays()
    {
        await using var db = TestDbContextFactory.Create();
        var creatorId = Guid.NewGuid();
        var spot = await SeedAsync(db, creatorId);

        var handler = CreateHandler(db, creatorId);
        var result = await handler.Handle(new SubmitSpotDeletion.Command(spot.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(SpotChangeRequestType.Deletion, result.Value.Type);
        Assert.Equal(SpotChangeRequestStatus.Pending, result.Value.Status);
        Assert.True(await db.Spots.AnyAsync(s => s.Id == spot.Id));
    }

    [Fact]
    public async Task Nonexistent_spot_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = CreateHandler(db, Guid.NewGuid());

        var result = await handler.Handle(new SubmitSpotDeletion.Command(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SpotsMessageKeys.NotFound, result.Error.Code);
        Assert.Equal(StatusCodes.Status404NotFound, result.Error.StatusCode);
    }

    [Fact]
    public async Task Non_creator_cannot_submit_a_deletion()
    {
        await using var db = TestDbContextFactory.Create();
        var creatorId = Guid.NewGuid();
        var spot = await SeedAsync(db, creatorId);

        var handler = CreateHandler(db, Guid.NewGuid());
        var result = await handler.Handle(new SubmitSpotDeletion.Command(spot.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SpotsMessageKeys.NotYourSpot, result.Error.Code);
        Assert.Equal(StatusCodes.Status403Forbidden, result.Error.StatusCode);
        Assert.False(await db.SpotChangeRequests.AnyAsync(r => r.SpotId == spot.Id));
    }
}
