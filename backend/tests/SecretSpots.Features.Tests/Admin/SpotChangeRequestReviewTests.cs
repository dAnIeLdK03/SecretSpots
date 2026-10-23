using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NetTopologySuite.Geometries;
using SecretSpots.Domain;
using SecretSpots.Features.Admin;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Admin;

public class SpotChangeRequestReviewTests
{
    private static async Task<(Spot Spot, SpotChangeRequest Request)> SeedAsync(IAppDbContext db, SpotChangeRequestType type)
    {
        var creatorId = Guid.NewGuid();
        var spot = new Spot
        {
            Id = Guid.NewGuid(),
            Name = "Original name",
            Description = "Original description",
            Category = SpotCategory.Nature,
            PhotoUrls = ["https://example.com/kept.jpg", "https://example.com/removed.jpg"],
            Location = new Point(23.3219, 42.6977) { SRID = 4326 },
            CreatedByUserId = creatorId,
        };
        var request = new SpotChangeRequest
        {
            Id = Guid.NewGuid(),
            SpotId = spot.Id,
            RequestedByUserId = creatorId,
            Type = type,
        };
        if (type == SpotChangeRequestType.Edit)
        {
            request.ProposedName = "New name";
            request.ProposedDescription = "New description";
            request.ProposedCategory = SpotCategory.Viewpoint;
            request.ProposedPhotoUrls = ["https://example.com/kept.jpg", "https://example.com/added.jpg"];
        }
        db.Spots.Add(spot);
        db.SpotChangeRequests.Add(request);
        await db.SaveChangesAsync();
        return (spot, request);
    }

    private static ApproveSpotChangeRequest.Handler CreateApproveHandler(IAppDbContext db, FakePhotoStorage photoStorage) =>
        new(db, new FakeUserContext(Guid.NewGuid(), UserRole.Admin), photoStorage, TestLocalizerFactory.Create(),
            NullLogger<ApproveSpotChangeRequest.Handler>.Instance);

    private static RejectSpotChangeRequest.Handler CreateRejectHandler(IAppDbContext db, FakePhotoStorage photoStorage) =>
        new(db, new FakeUserContext(Guid.NewGuid(), UserRole.Admin), photoStorage, TestLocalizerFactory.Create(),
            NullLogger<RejectSpotChangeRequest.Handler>.Instance);

    [Fact]
    public async Task Approving_an_edit_applies_it_and_deletes_only_the_removed_photos()
    {
        await using var db = TestDbContextFactory.Create();
        var (spot, request) = await SeedAsync(db, SpotChangeRequestType.Edit);
        var photoStorage = new FakePhotoStorage();

        var result = await CreateApproveHandler(db, photoStorage).Handle(
            new ApproveSpotChangeRequest.Command(request.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        db.ChangeTracker.Clear();
        var saved = await db.Spots.SingleAsync(s => s.Id == spot.Id);
        Assert.Equal("New name", saved.Name);
        Assert.Equal(SpotCategory.Viewpoint, saved.Category);
        Assert.Equal(["https://example.com/kept.jpg", "https://example.com/added.jpg"], saved.PhotoUrls);
        Assert.Equal(spot.Location.X, saved.Location.X, precision: 6);
        Assert.Equal(["https://example.com/removed.jpg"], photoStorage.DeletedUrls);
        Assert.Equal(SpotChangeRequestStatus.Approved, (await db.SpotChangeRequests.SingleAsync(r => r.Id == request.Id)).Status);
    }

    [Fact]
    public async Task Approving_a_deletion_deletes_the_spot_and_resolves_open_reports()
    {
        await using var db = TestDbContextFactory.Create();
        var (spot, request) = await SeedAsync(db, SpotChangeRequestType.Deletion);

        var comment = new Comment { Id = Guid.NewGuid(), SpotId = spot.Id, UserId = Guid.NewGuid(), Text = "test comment" };
        db.Comments.Add(comment);
        var spotReport = new Report
        {
            Id = Guid.NewGuid(), ContentType = ReportedContentType.Spot, ContentId = spot.Id,
            ReporterUserId = Guid.NewGuid(), Reason = ReportReason.Spam,
        };
        var commentReport = new Report
        {
            Id = Guid.NewGuid(), ContentType = ReportedContentType.Comment, ContentId = comment.Id,
            ReporterUserId = Guid.NewGuid(), Reason = ReportReason.Inappropriate,
        };
        db.Reports.AddRange(spotReport, commentReport);
        await db.SaveChangesAsync();
        var photoStorage = new FakePhotoStorage();

        var result = await CreateApproveHandler(db, photoStorage).Handle(
            new ApproveSpotChangeRequest.Command(request.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        // ExecuteUpdate/ExecuteDelete bypass the change tracker, so clear it before re-querying.
        db.ChangeTracker.Clear();
        Assert.False(await db.Spots.AnyAsync(s => s.Id == spot.Id));
        Assert.False(await db.SpotChangeRequests.AnyAsync(r => r.SpotId == spot.Id));
        Assert.Equal(spot.PhotoUrls, photoStorage.DeletedUrls);
        Assert.Equal(ReportResolutionAction.ContentDeletedByAuthor, (await db.Reports.SingleAsync(r => r.Id == spotReport.Id)).ResolutionAction);
        Assert.Equal(ReportResolutionAction.ContentDeletedByAuthor, (await db.Reports.SingleAsync(r => r.Id == commentReport.Id)).ResolutionAction);
    }

    [Fact]
    public async Task Rejecting_an_edit_keeps_the_spot_and_deletes_the_unused_uploads()
    {
        await using var db = TestDbContextFactory.Create();
        var (spot, request) = await SeedAsync(db, SpotChangeRequestType.Edit);
        var photoStorage = new FakePhotoStorage();

        var result = await CreateRejectHandler(db, photoStorage).Handle(
            new RejectSpotChangeRequest.Command(request.Id, "Not an improvement"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        db.ChangeTracker.Clear();
        Assert.Equal("Original name", (await db.Spots.SingleAsync(s => s.Id == spot.Id)).Name);
        Assert.Equal(["https://example.com/added.jpg"], photoStorage.DeletedUrls);
        var saved = await db.SpotChangeRequests.SingleAsync(r => r.Id == request.Id);
        Assert.Equal(SpotChangeRequestStatus.Rejected, saved.Status);
        Assert.Equal("Not an improvement", saved.RejectionReason);
    }

    [Fact]
    public async Task Reviewing_an_already_reviewed_request_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();
        var (_, request) = await SeedAsync(db, SpotChangeRequestType.Edit);
        var photoStorage = new FakePhotoStorage();
        await CreateRejectHandler(db, photoStorage).Handle(
            new RejectSpotChangeRequest.Command(request.Id, "No"), CancellationToken.None);

        var result = await CreateApproveHandler(db, photoStorage).Handle(
            new ApproveSpotChangeRequest.Command(request.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AdminMessageKeys.ChangeRequestNotFound, result.Error.Code);
    }
}
