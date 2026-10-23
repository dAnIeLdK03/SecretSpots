using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NetTopologySuite.Geometries;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Spots;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Spots;

public class DeleteSpotTests
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

    private static DeleteSpot.Handler CreateHandler(IAppDbContext db, Guid userId, FakePhotoStorage? photoStorage = null) =>
        new(db, new FakeUserContext(userId), photoStorage ?? new FakePhotoStorage(), TestLocalizerFactory.Create(), NullLogger<DeleteSpot.Handler>.Instance);

    [Fact]
    public async Task Creator_can_delete_their_spot()
    {
        await using var db = TestDbContextFactory.Create();
        var creatorId = Guid.NewGuid();
        var spot = await SeedAsync(db, creatorId);
        var photoStorage = new FakePhotoStorage();

        var handler = CreateHandler(db, creatorId, photoStorage);
        var result = await handler.Handle(new DeleteSpot.Command(spot.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(await db.Spots.AnyAsync(s => s.Id == spot.Id));
        Assert.Equal(spot.PhotoUrls, photoStorage.DeletedUrls);
    }

    [Fact]
    public async Task Nonexistent_spot_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = CreateHandler(db, Guid.NewGuid());

        var result = await handler.Handle(new DeleteSpot.Command(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SpotsMessageKeys.NotFound, result.Error.Code);
        Assert.Equal(StatusCodes.Status404NotFound, result.Error.StatusCode);
    }

    [Fact]
    public async Task Deleting_a_spot_resolves_open_reports_against_it_and_its_comments()
    {
        await using var db = TestDbContextFactory.Create();
        var creatorId = Guid.NewGuid();
        var spot = await SeedAsync(db, creatorId);

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            SpotId = spot.Id,
            UserId = Guid.NewGuid(),
            Text = "test comment",
        };
        db.Comments.Add(comment);

        var spotReport = new Report
        {
            Id = Guid.NewGuid(),
            ContentType = ReportedContentType.Spot,
            ContentId = spot.Id,
            ReporterUserId = Guid.NewGuid(),
            Reason = ReportReason.Spam,
        };
        var commentReport = new Report
        {
            Id = Guid.NewGuid(),
            ContentType = ReportedContentType.Comment,
            ContentId = comment.Id,
            ReporterUserId = Guid.NewGuid(),
            Reason = ReportReason.Inappropriate,
        };
        db.Reports.AddRange(spotReport, commentReport);
        await db.SaveChangesAsync();

        var handler = CreateHandler(db, creatorId);
        var result = await handler.Handle(new DeleteSpot.Command(spot.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);

        // ExecuteUpdateAsync writes straight to the database, bypassing the change tracker — the
        // Report instances seeded above are still tracked with their pre-update (null ResolvedAt)
        // values, so a re-query without clearing would just hand back those stale tracked
        // instances instead of hitting the database.
        db.ChangeTracker.Clear();
        var savedSpotReport = await db.Reports.SingleAsync(r => r.Id == spotReport.Id);
        Assert.NotNull(savedSpotReport.ResolvedAt);
        Assert.Equal(ReportResolutionAction.ContentDeletedByAuthor, savedSpotReport.ResolutionAction);
        Assert.Null(savedSpotReport.ResolvedByUserId);

        var savedCommentReport = await db.Reports.SingleAsync(r => r.Id == commentReport.Id);
        Assert.NotNull(savedCommentReport.ResolvedAt);
        Assert.Equal(ReportResolutionAction.ContentDeletedByAuthor, savedCommentReport.ResolutionAction);
        Assert.Null(savedCommentReport.ResolvedByUserId);
    }

    [Fact]
    public async Task Non_creator_cannot_delete_the_spot()
    {
        await using var db = TestDbContextFactory.Create();
        var creatorId = Guid.NewGuid();
        var spot = await SeedAsync(db, creatorId);

        var handler = CreateHandler(db, Guid.NewGuid());
        var result = await handler.Handle(new DeleteSpot.Command(spot.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SpotsMessageKeys.NotYourSpot, result.Error.Code);
        Assert.Equal(StatusCodes.Status403Forbidden, result.Error.StatusCode);

        Assert.True(await db.Spots.AnyAsync(s => s.Id == spot.Id));
    }
}
