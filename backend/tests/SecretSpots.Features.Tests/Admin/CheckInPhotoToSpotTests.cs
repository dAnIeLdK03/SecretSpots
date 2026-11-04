using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SecretSpots.Domain;
using SecretSpots.Features.Admin;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Spots;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Admin;

// An admin adding a visitor's check-in photo to the spot, and what happens to that photo after.
public class CheckInPhotoToSpotTests
{
    private static string UploadBy(Guid userId, string name) => $"{TestOptionsFactory.PhotoPublicBaseUrl}/{userId}/{name}";

    private static async Task<CheckIn> SeedCheckInAsync(IAppDbContext db, Spot spot, User visitor, string? photoUrl = null)
    {
        var checkIn = new CheckIn
        {
            Id = Guid.NewGuid(), SpotId = spot.Id, UserId = visitor.Id, PhotoUrl = photoUrl ?? UploadBy(visitor.Id, $"{Guid.NewGuid():N}.webp"),
        };
        db.CheckIns.Add(checkIn);
        await db.SaveChangesAsync();
        return checkIn;
    }

    private static AddCheckInPhotoToSpot.Handler CreateAddHandler(IAppDbContext db) =>
        new(db, new FakeUserContext(Guid.NewGuid(), UserRole.Admin), TestLocalizerFactory.Create(), NullLogger<AddCheckInPhotoToSpot.Handler>.Instance);

    [Fact]
    public async Task Adding_puts_the_photo_on_the_spot_and_tells_the_visitor_once()
    {
        await using var db = TestDbContextFactory.Create();
        var visitor = await TestUserFactory.SeedAsync(db);
        var spot = await TestSpotFactory.SeedAsync(db);
        var checkIn = await SeedCheckInAsync(db, spot, visitor);
        List<string> originalPhotos = [.. spot.PhotoUrls];
        var handler = CreateAddHandler(db);

        var first = await handler.Handle(new AddCheckInPhotoToSpot.Command(checkIn.Id), CancellationToken.None);
        var again = await handler.Handle(new AddCheckInPhotoToSpot.Command(checkIn.Id), CancellationToken.None);

        Assert.True(first.IsSuccess && again.IsSuccess);
        db.ChangeTracker.Clear();
        Assert.Equal([.. originalPhotos, checkIn.PhotoUrl], (await db.Spots.SingleAsync(s => s.Id == spot.Id)).PhotoUrls);
        var notification = await db.Notifications.SingleAsync(n => n.UserId == visitor.Id);
        Assert.Equal((NotificationType.CheckInPhotoAddedToSpot, spot.Id, spot.Name), (notification.Type, notification.RelatedSpotId, notification.RelatedSpotName));
    }

    [Fact]
    public async Task A_spot_that_is_full_takes_no_more_photos()
    {
        await using var db = TestDbContextFactory.Create();
        var visitor = await TestUserFactory.SeedAsync(db);
        var spot = await TestSpotFactory.SeedAsync(
            db, photoUrls: Enumerable.Range(1, CreateSpot.MaxPhotoCount).Select(i => $"https://example.com/{i}.jpg").ToList());
        var checkIn = await SeedCheckInAsync(db, spot, visitor);

        var result = await CreateAddHandler(db).Handle(new AddCheckInPhotoToSpot.Command(checkIn.Id), CancellationToken.None);

        Assert.Equal(AdminMessageKeys.SpotPhotoLimitReached, result.Error.Code);
        Assert.False(await db.Notifications.AnyAsync(n => n.UserId == visitor.Id));
    }

    [Fact]
    public async Task Adding_from_a_missing_check_in_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await CreateAddHandler(db).Handle(new AddCheckInPhotoToSpot.Command(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(AdminMessageKeys.CheckInNotFound, result.Error.Code);
    }

    [Fact]
    public async Task The_admin_list_shows_a_spots_check_ins_newest_first_and_which_photos_are_already_on_it()
    {
        await using var db = TestDbContextFactory.Create();
        var visitor = await TestUserFactory.SeedAsync(db, displayName: "Visitor Vera");
        var spot = await TestSpotFactory.SeedAsync(db);
        var older = await SeedCheckInAsync(db, spot, visitor);
        var newer = await SeedCheckInAsync(db, spot, visitor);
        await CreateAddHandler(db).Handle(new AddCheckInPhotoToSpot.Command(older.Id), CancellationToken.None);
        await SeedCheckInAsync(db, await TestSpotFactory.SeedAsync(db), visitor);
        var handler = new GetAdminSpotCheckIns.Handler(db, TestLocalizerFactory.Create());

        var result = await handler.Handle(new GetAdminSpotCheckIns.Query(spot.Id, 1, 20), CancellationToken.None);
        var missing = await handler.Handle(new GetAdminSpotCheckIns.Query(Guid.NewGuid(), 1, 20), CancellationToken.None);

        Assert.Equal([newer.Id, older.Id], result.Value.Items.Select(c => c.Id));
        Assert.Equal([false, true], result.Value.Items.Select(c => c.IsOnSpot));
        Assert.All(result.Value.Items, c => Assert.Equal("Visitor Vera", c.UserDisplayName));
        Assert.Equal(AdminMessageKeys.SpotNotFound, missing.Error.Code);
    }

    [Fact]
    public async Task Dropping_the_visitors_photo_in_an_approved_edit_keeps_their_file_but_deletes_the_owners()
    {
        await using var db = TestDbContextFactory.Create();
        var owner = await TestUserFactory.SeedAsync(db);
        var visitor = await TestUserFactory.SeedAsync(db);
        var kept = UploadBy(owner.Id, "kept.webp");
        var ownersDropped = UploadBy(owner.Id, "dropped.webp");
        var spot = await TestSpotFactory.SeedAsync(db, owner.Id, photoUrls: [kept, ownersDropped]);
        var checkIn = await SeedCheckInAsync(db, spot, visitor);
        await CreateAddHandler(db).Handle(new AddCheckInPhotoToSpot.Command(checkIn.Id), CancellationToken.None);
        var request = new SpotChangeRequest
        {
            Id = Guid.NewGuid(), SpotId = spot.Id, RequestedByUserId = owner.Id, Type = SpotChangeRequestType.Edit,
            ProposedName = spot.Name, ProposedDescription = spot.Description, ProposedCategory = spot.Category, ProposedPhotoUrls = [kept],
        };
        db.SpotChangeRequests.Add(request);
        await db.SaveChangesAsync();
        var photoStorage = new FakePhotoStorage();

        var result = await new ApproveSpotChangeRequest.Handler(
                db, new FakeUserContext(Guid.NewGuid(), UserRole.Admin), photoStorage, TimeProvider.System,
                TestLocalizerFactory.Create(), NullLogger<ApproveSpotChangeRequest.Handler>.Instance)
            .Handle(new ApproveSpotChangeRequest.Command(request.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal([ownersDropped], photoStorage.DeletedUrls);
        Assert.True(await db.CheckIns.AnyAsync(c => c.Id == checkIn.Id && c.PhotoUrl == checkIn.PhotoUrl));
    }
}
