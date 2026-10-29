using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Reports;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Reports;

// The admin side of reports: listing, dismissing and deleting the reported content.
public class ReportModerationTests
{
    private static async Task<Report> SeedReportAsync(
        IAppDbContext db, ReportedContentType contentType, Guid contentId, Guid? reporterId = null)
    {
        var report = new Report
        {
            Id = Guid.NewGuid(),
            ContentType = contentType,
            ContentId = contentId,
            ReporterUserId = reporterId ?? Guid.NewGuid(),
            Reason = ReportReason.Spam,
        };
        db.Reports.Add(report);
        await db.SaveChangesAsync();
        return report;
    }

    private static DeleteReportedContent.Handler CreateDeleteHandler(IAppDbContext db, Guid adminId, FakePhotoStorage photoStorage) =>
        new(db, new FakeUserContext(adminId, UserRole.Admin), photoStorage, TestLocalizerFactory.Create(),
            NullLogger<DeleteReportedContent.Handler>.Instance);

    [Fact]
    public async Task Dismissing_a_report_resolves_it_without_touching_the_content()
    {
        await using var db = TestDbContextFactory.Create();
        var adminId = Guid.NewGuid();
        var spot = await TestSpotFactory.SeedAsync(db);
        var report = await SeedReportAsync(db, ReportedContentType.Spot, spot.Id);

        var result = await new DismissReport.Handler(
                db, new FakeUserContext(adminId, UserRole.Admin), TestLocalizerFactory.Create(), NullLogger<DismissReport.Handler>.Instance)
            .Handle(new DismissReport.Command(report.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        db.ChangeTracker.Clear();
        var saved = await db.Reports.SingleAsync(r => r.Id == report.Id);
        Assert.Equal(ReportResolutionAction.Dismissed, saved.ResolutionAction);
        Assert.Equal(adminId, saved.ResolvedByUserId);
        Assert.NotNull(saved.ResolvedAt);
        Assert.True(await db.Spots.AnyAsync(s => s.Id == spot.Id));
    }

    [Fact]
    public async Task Dismissing_a_missing_report_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await new DismissReport.Handler(
                db, new FakeUserContext(Guid.NewGuid(), UserRole.Admin), TestLocalizerFactory.Create(), NullLogger<DismissReport.Handler>.Instance)
            .Handle(new DismissReport.Command(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReportsMessageKeys.NotFound, result.Error.Code);
    }

    [Fact]
    public async Task Deleting_a_reported_spot_removes_it_resolves_every_open_report_and_notifies_the_author()
    {
        await using var db = TestDbContextFactory.Create();
        var adminId = Guid.NewGuid();
        var author = await TestUserFactory.SeedAsync(db);
        var spot = await TestSpotFactory.SeedAsync(db, author.Id, photoUrls: ["https://example.com/a.jpg", "https://example.com/b.jpg"]);
        var clicked = await SeedReportAsync(db, ReportedContentType.Spot, spot.Id);
        var other = await SeedReportAsync(db, ReportedContentType.Spot, spot.Id);
        var photoStorage = new FakePhotoStorage();

        var result = await CreateDeleteHandler(db, adminId, photoStorage).Handle(
            new DeleteReportedContent.Command(clicked.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        db.ChangeTracker.Clear();
        Assert.False(await db.Spots.AnyAsync(s => s.Id == spot.Id));
        Assert.Equal(spot.PhotoUrls, photoStorage.DeletedUrls);
        foreach (var id in new[] { clicked.Id, other.Id })
        {
            var saved = await db.Reports.SingleAsync(r => r.Id == id);
            Assert.Equal(ReportResolutionAction.ContentDeleted, saved.ResolutionAction);
            Assert.Equal(adminId, saved.ResolvedByUserId);
        }
        Assert.True(await db.Notifications.AnyAsync(n => n.UserId == author.Id && n.Type == NotificationType.YourContentRemoved));
    }

    [Fact]
    public async Task Deleting_a_reported_comment_soft_deletes_it_and_notifies_the_author()
    {
        await using var db = TestDbContextFactory.Create();
        var author = await TestUserFactory.SeedAsync(db);
        var spot = await TestSpotFactory.SeedAsync(db);
        var comment = await TestCommentFactory.SeedAsync(db, spot.Id, author.Id);
        var report = await SeedReportAsync(db, ReportedContentType.Comment, comment.Id);

        var result = await CreateDeleteHandler(db, Guid.NewGuid(), new FakePhotoStorage()).Handle(
            new DeleteReportedContent.Command(report.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        db.ChangeTracker.Clear();
        Assert.True((await db.Comments.SingleAsync(c => c.Id == comment.Id)).IsDeleted);
        Assert.True(await db.Spots.AnyAsync(s => s.Id == spot.Id));
        Assert.Equal(ReportResolutionAction.ContentDeleted, (await db.Reports.SingleAsync(r => r.Id == report.Id)).ResolutionAction);
        Assert.True(await db.Notifications.AnyAsync(n =>
            n.UserId == author.Id && n.Type == NotificationType.YourContentRemoved && n.RelatedSpotId == spot.Id));
    }

    [Fact]
    public async Task Deleting_content_that_is_already_gone_still_resolves_the_report()
    {
        await using var db = TestDbContextFactory.Create();
        var report = await SeedReportAsync(db, ReportedContentType.Spot, Guid.NewGuid());

        var result = await CreateDeleteHandler(db, Guid.NewGuid(), new FakePhotoStorage()).Handle(
            new DeleteReportedContent.Command(report.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        db.ChangeTracker.Clear();
        Assert.Equal(ReportResolutionAction.ContentDeleted, (await db.Reports.SingleAsync(r => r.Id == report.Id)).ResolutionAction);
    }

    [Fact]
    public async Task Admin_removing_their_own_comment_does_not_notify_themselves()
    {
        await using var db = TestDbContextFactory.Create();
        var admin = await TestUserFactory.SeedAsync(db, UserRole.Admin);
        var spot = await TestSpotFactory.SeedAsync(db);
        var comment = await TestCommentFactory.SeedAsync(db, spot.Id, admin.Id);
        var report = await SeedReportAsync(db, ReportedContentType.Comment, comment.Id);

        await CreateDeleteHandler(db, admin.Id, new FakePhotoStorage()).Handle(
            new DeleteReportedContent.Command(report.Id), CancellationToken.None);

        Assert.False(await db.Notifications.AnyAsync(n => n.UserId == admin.Id && n.Type == NotificationType.YourContentRemoved));
    }

    [Fact]
    public async Task Admin_list_hides_resolved_reports_unless_asked_and_resolves_previews()
    {
        await using var db = TestDbContextFactory.Create();
        var reporter = await TestUserFactory.SeedAsync(db, displayName: "Reporter Rita");
        var spot = await TestSpotFactory.SeedAsync(db);
        var comment = await TestCommentFactory.SeedAsync(db, spot.Id, Guid.NewGuid(), text: "rude words");
        var open = await SeedReportAsync(db, ReportedContentType.Comment, comment.Id, reporter.Id);
        var resolved = await SeedReportAsync(db, ReportedContentType.Spot, spot.Id);
        resolved.ResolvedAt = DateTimeOffset.UtcNow;
        resolved.ResolutionAction = ReportResolutionAction.Dismissed;
        await db.SaveChangesAsync();
        var handler = new GetAdminReports.Handler(db);

        var openOnly = await handler.Handle(new GetAdminReports.Query(1, 100, IncludeResolved: false), CancellationToken.None);
        var all = await handler.Handle(new GetAdminReports.Query(1, 100, IncludeResolved: true), CancellationToken.None);

        var item = Assert.Single(openOnly.Items, r => r.Id == open.Id);
        Assert.Equal("rude words", item.ContentPreview);
        Assert.Equal(spot.Id, item.RelatedSpotId);
        Assert.Equal("Reporter Rita", item.ReporterDisplayName);
        Assert.DoesNotContain(openOnly.Items, r => r.Id == resolved.Id);
        var resolvedItem = Assert.Single(all.Items, r => r.Id == resolved.Id);
        Assert.Equal(spot.Name, resolvedItem.ContentPreview);
    }
}
