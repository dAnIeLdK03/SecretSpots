using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Spots;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Spots;

// Who can still see a rejected spot (its creator and admins) and that a soft-deleted one is gone
// for everyone — across the direct lookup and both searches.
public class SpotVisibilityTests
{
    private static async Task<bool> CanOpenAsync(IAppDbContext db, Spot spot, IUserContext viewer)
    {
        var result = await new GetSpot.Handler(db, viewer, TestLocalizerFactory.Create())
            .Handle(new GetSpot.Query(spot.Id), CancellationToken.None);
        if (!result.IsSuccess)
        {
            // Hidden spots must look exactly like ones that never existed.
            Assert.Equal(SpotsMessageKeys.NotFound, result.Error.Code);
        }

        return result.IsSuccess;
    }

    private static async Task<Spot> SeedSoftDeletedAsync(IAppDbContext db, double longitude = 23.3219, double latitude = 42.6977)
    {
        var spot = await TestSpotFactory.SeedAsync(
            db, status: SpotModerationStatus.Rejected, name: $"Deleted {Guid.NewGuid():N}", longitude: longitude, latitude: latitude);
        spot.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return spot;
    }

    [Fact]
    public async Task A_rejected_spot_opens_only_for_its_creator_and_admins()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await TestSpotFactory.SeedAsync(db, status: SpotModerationStatus.Rejected);

        Assert.False(await CanOpenAsync(db, spot, new AnonymousUserContext()));
        Assert.False(await CanOpenAsync(db, spot, new FakeUserContext(Guid.NewGuid())));
        Assert.True(await CanOpenAsync(db, spot, new FakeUserContext(spot.CreatedByUserId)));
        Assert.True(await CanOpenAsync(db, spot, new FakeUserContext(Guid.NewGuid(), UserRole.Admin)));
    }

    [Theory]
    [InlineData(SpotModerationStatus.Pending)]
    [InlineData(SpotModerationStatus.Approved)]
    public async Task Pending_and_approved_spots_open_for_anyone_including_anonymous(SpotModerationStatus status)
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await TestSpotFactory.SeedAsync(db, status: status);

        Assert.True(await CanOpenAsync(db, spot, new AnonymousUserContext()));
    }

    [Fact]
    public async Task A_soft_deleted_spot_is_gone_even_for_its_creator_and_admins()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await SeedSoftDeletedAsync(db);

        Assert.False(await CanOpenAsync(db, spot, new FakeUserContext(spot.CreatedByUserId)));
        Assert.False(await CanOpenAsync(db, spot, new FakeUserContext(Guid.NewGuid(), UserRole.Admin)));
    }

    [Fact]
    public async Task Text_search_leaves_out_soft_deleted_spots()
    {
        await using var db = TestDbContextFactory.Create();
        var marker = "v" + Guid.NewGuid().ToString("N")[..12];
        var visible = await TestSpotFactory.SeedAsync(db, name: $"Visible {marker}");
        var deleted = await SeedSoftDeletedAsync(db);
        deleted.Name = $"Deleted {marker}";
        deleted.ModerationStatus = SpotModerationStatus.Approved;
        await db.SaveChangesAsync();

        var result = await new SearchSpots.Handler(db).Handle(new SearchSpots.Query(marker, null, 1, 100), CancellationToken.None);

        Assert.Equal([visible.Id], result.Items.Select(s => s.Id));
    }

    [Fact]
    public async Task Nearby_search_leaves_out_rejected_and_soft_deleted_spots()
    {
        await using var db = TestDbContextFactory.Create();
        // Random center, same reason as SearchNearbySpotsHandlerTests: the shared DB fills up
        // around any fixed point.
        var lat = Random.Shared.NextDouble() * 120 - 60;
        var lng = Random.Shared.NextDouble() * 300 - 150;
        var approved = await TestSpotFactory.SeedAsync(db, longitude: lng, latitude: lat);
        var pending = await TestSpotFactory.SeedAsync(db, status: SpotModerationStatus.Pending, longitude: lng, latitude: lat);
        var rejected = await TestSpotFactory.SeedAsync(db, status: SpotModerationStatus.Rejected, longitude: lng, latitude: lat);
        var deleted = await SeedSoftDeletedAsync(db, lng, lat);
        deleted.ModerationStatus = SpotModerationStatus.Approved;
        await db.SaveChangesAsync();

        var result = await new SearchNearbySpots.Handler(db).Handle(new SearchNearbySpots.Query(lat, lng, 1), CancellationToken.None);

        var ids = result.Items.Select(s => s.Id).ToHashSet();
        Assert.Contains(approved.Id, ids);
        Assert.Contains(pending.Id, ids);
        Assert.DoesNotContain(rejected.Id, ids);
        Assert.DoesNotContain(deleted.Id, ids);
    }
}
