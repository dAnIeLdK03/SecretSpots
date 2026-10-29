using Microsoft.EntityFrameworkCore;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.SavedSpots;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.SavedSpots;

public class SavedSpotsTests
{
    private static SaveSpot.Handler CreateSaveHandler(IAppDbContext db, Guid userId, UserRole role = UserRole.User) =>
        new(db, new FakeUserContext(userId, role), TestLocalizerFactory.Create());

    [Fact]
    public async Task Saving_a_spot_marks_it_saved_for_that_user_only()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var spot = await TestSpotFactory.SeedAsync(db);

        var result = await CreateSaveHandler(db, userId).Handle(new SaveSpot.Command(spot.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(await new GetIsSpotSaved.Handler(db, new FakeUserContext(userId))
            .Handle(new GetIsSpotSaved.Query(spot.Id), CancellationToken.None));
        Assert.False(await new GetIsSpotSaved.Handler(db, new FakeUserContext(Guid.NewGuid()))
            .Handle(new GetIsSpotSaved.Query(spot.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Saving_twice_is_idempotent()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var spot = await TestSpotFactory.SeedAsync(db);
        var handler = CreateSaveHandler(db, userId);
        await handler.Handle(new SaveSpot.Command(spot.Id), CancellationToken.None);

        var result = await handler.Handle(new SaveSpot.Command(spot.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, await db.SavedSpots.CountAsync(s => s.SpotId == spot.Id && s.UserId == userId));
    }

    [Fact]
    public async Task Saving_a_missing_spot_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await CreateSaveHandler(db, Guid.NewGuid()).Handle(new SaveSpot.Command(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SavedSpotsMessageKeys.SpotNotFound, result.Error.Code);
    }

    [Fact]
    public async Task Admins_cannot_save_spots()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await TestSpotFactory.SeedAsync(db);

        var result = await CreateSaveHandler(db, Guid.NewGuid(), UserRole.Admin).Handle(new SaveSpot.Command(spot.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SavedSpotsMessageKeys.AdminsCannotSaveSpots, result.Error.Code);
    }

    [Fact]
    public async Task Unsaving_removes_only_the_callers_save_and_is_a_no_op_when_not_saved()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var spot = await TestSpotFactory.SeedAsync(db);
        await CreateSaveHandler(db, userId).Handle(new SaveSpot.Command(spot.Id), CancellationToken.None);
        await CreateSaveHandler(db, otherUserId).Handle(new SaveSpot.Command(spot.Id), CancellationToken.None);
        var unsave = new UnsaveSpot.Handler(db, new FakeUserContext(userId));

        var first = await unsave.Handle(new UnsaveSpot.Command(spot.Id), CancellationToken.None);
        var second = await unsave.Handle(new UnsaveSpot.Command(spot.Id), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.False(await db.SavedSpots.AnyAsync(s => s.SpotId == spot.Id && s.UserId == userId));
        Assert.True(await db.SavedSpots.AnyAsync(s => s.SpotId == spot.Id && s.UserId == otherUserId));
    }

    [Fact]
    public async Task My_saved_spots_lists_newest_first_and_skips_deleted_spots()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var older = await TestSpotFactory.SeedAsync(db, name: "Older save");
        var newer = await TestSpotFactory.SeedAsync(db, name: "Newer save", photoUrls: []);
        var deleted = await TestSpotFactory.SeedAsync(db);
        var handler = CreateSaveHandler(db, userId);
        await handler.Handle(new SaveSpot.Command(older.Id), CancellationToken.None);
        await handler.Handle(new SaveSpot.Command(deleted.Id), CancellationToken.None);
        await handler.Handle(new SaveSpot.Command(newer.Id), CancellationToken.None);
        deleted.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        var page = await new GetMySavedSpots.Handler(db, new FakeUserContext(userId))
            .Handle(new GetMySavedSpots.Query(1, 10), CancellationToken.None);

        Assert.Equal([newer.Id, older.Id], page.Items.Select(i => i.SpotId));
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(string.Empty, page.Items[0].PhotoUrl);
        Assert.Equal(older.PhotoUrls[0], page.Items[1].PhotoUrl);
    }
}
