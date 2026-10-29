using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Spots;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Spots;

// The owner's side: My spots, and checking on / withdrawing a pending edit or deletion.
public class MySpotsTests
{
    private static async Task<SpotChangeRequest> SeedRequestAsync(
        IAppDbContext db, Spot spot, SpotChangeRequestStatus status = SpotChangeRequestStatus.Pending,
        List<string>? proposedPhotoUrls = null, DateTimeOffset createdAt = default)
    {
        var request = new SpotChangeRequest
        {
            Id = Guid.NewGuid(),
            SpotId = spot.Id,
            RequestedByUserId = spot.CreatedByUserId,
            Type = SpotChangeRequestType.Edit,
            Status = status,
            ProposedName = "Proposed",
            ProposedDescription = "Proposed description",
            ProposedCategory = SpotCategory.Viewpoint,
            ProposedPhotoUrls = proposedPhotoUrls ?? spot.PhotoUrls,
            CreatedAt = createdAt,
        };
        db.SpotChangeRequests.Add(request);
        await db.SaveChangesAsync();
        return request;
    }

    private static CancelSpotChangeRequest.Handler CreateCancelHandler(IAppDbContext db, Guid userId, FakePhotoStorage photoStorage) =>
        new(db, new FakeUserContext(userId), photoStorage, TestLocalizerFactory.Create(), NullLogger<CancelSpotChangeRequest.Handler>.Instance);

    [Fact]
    public async Task My_spots_shows_every_status_newest_first_but_not_soft_deleted_or_other_users_spots()
    {
        await using var db = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var pending = await TestSpotFactory.SeedAsync(db, ownerId, SpotModerationStatus.Pending);
        var rejected = await TestSpotFactory.SeedAsync(db, ownerId, SpotModerationStatus.Rejected);
        rejected.RejectionReason = "Blurry";
        var softDeleted = await TestSpotFactory.SeedAsync(db, ownerId, SpotModerationStatus.Rejected);
        softDeleted.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        await TestSpotFactory.SeedAsync(db);

        var page = await new GetMySpots.Handler(db, new FakeUserContext(ownerId)).Handle(new GetMySpots.Query(1, 20), CancellationToken.None);

        Assert.Equal([rejected.Id, pending.Id], page.Items.Select(s => s.Id));
        Assert.Equal("Blurry", page.Items[0].RejectionReason);
    }

    [Fact]
    public async Task Owner_sees_their_latest_change_request()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await TestSpotFactory.SeedAsync(db);
        await SeedRequestAsync(db, spot, SpotChangeRequestStatus.Rejected, createdAt: DateTimeOffset.UtcNow.AddHours(-1));
        var latest = await SeedRequestAsync(db, spot);

        var result = await new GetMySpotChangeRequest.Handler(db, new FakeUserContext(spot.CreatedByUserId), TestLocalizerFactory.Create())
            .Handle(new GetMySpotChangeRequest.Query(spot.Id), CancellationToken.None);

        Assert.Equal(latest.Id, result.Value.Id);
        Assert.Equal(SpotChangeRequestStatus.Pending, result.Value.Status);
    }

    [Fact]
    public async Task Change_request_lookup_is_owner_only_and_404s_when_there_is_none()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await TestSpotFactory.SeedAsync(db);
        var localizer = TestLocalizerFactory.Create();

        var none = await new GetMySpotChangeRequest.Handler(db, new FakeUserContext(spot.CreatedByUserId), localizer)
            .Handle(new GetMySpotChangeRequest.Query(spot.Id), CancellationToken.None);
        var stranger = await new GetMySpotChangeRequest.Handler(db, new FakeUserContext(Guid.NewGuid()), localizer)
            .Handle(new GetMySpotChangeRequest.Query(spot.Id), CancellationToken.None);

        Assert.Equal(SpotsMessageKeys.ChangeRequestNotFound, none.Error.Code);
        Assert.Equal(SpotsMessageKeys.NotYourSpot, stranger.Error.Code);
    }

    [Fact]
    public async Task Cancelling_withdraws_the_pending_request_and_deletes_only_its_new_uploads()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await TestSpotFactory.SeedAsync(db, photoUrls: ["https://example.com/kept.jpg"]);
        var request = await SeedRequestAsync(db, spot, proposedPhotoUrls: ["https://example.com/kept.jpg", "https://example.com/new.jpg"]);
        var photoStorage = new FakePhotoStorage();

        var result = await CreateCancelHandler(db, spot.CreatedByUserId, photoStorage).Handle(
            new CancelSpotChangeRequest.Command(spot.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(await db.SpotChangeRequests.AnyAsync(r => r.Id == request.Id));
        Assert.Equal(["https://example.com/new.jpg"], photoStorage.DeletedUrls);
        Assert.Equal("https://example.com/kept.jpg", Assert.Single((await db.Spots.SingleAsync(s => s.Id == spot.Id)).PhotoUrls));
    }

    [Fact]
    public async Task Cancelling_needs_a_pending_request_on_your_own_spot()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await TestSpotFactory.SeedAsync(db);
        await SeedRequestAsync(db, spot, SpotChangeRequestStatus.Approved);
        var photoStorage = new FakePhotoStorage();

        var onlyReviewed = await CreateCancelHandler(db, spot.CreatedByUserId, photoStorage).Handle(
            new CancelSpotChangeRequest.Command(spot.Id), CancellationToken.None);
        var stranger = await CreateCancelHandler(db, Guid.NewGuid(), photoStorage).Handle(
            new CancelSpotChangeRequest.Command(spot.Id), CancellationToken.None);
        var missingSpot = await CreateCancelHandler(db, spot.CreatedByUserId, photoStorage).Handle(
            new CancelSpotChangeRequest.Command(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(SpotsMessageKeys.ChangeRequestNotFound, onlyReviewed.Error.Code);
        Assert.Equal(SpotsMessageKeys.NotYourSpot, stranger.Error.Code);
        Assert.Equal(SpotsMessageKeys.NotFound, missingSpot.Error.Code);
        Assert.Empty(photoStorage.DeletedUrls);
    }
}
