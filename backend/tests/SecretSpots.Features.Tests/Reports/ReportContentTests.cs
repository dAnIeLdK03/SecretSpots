using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Reports;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Reports;

public class ReportContentTests
{
    private static ReportSpot.Handler CreateSpotHandler(IAppDbContext db, Guid userId, UserRole role = UserRole.User) =>
        new(db, new FakeUserContext(userId, role), TestLocalizerFactory.Create(), NullLogger<ReportSpot.Handler>.Instance);

    private static ReportComment.Handler CreateCommentHandler(IAppDbContext db, Guid userId) =>
        new(db, new FakeUserContext(userId), TestLocalizerFactory.Create(), NullLogger<ReportComment.Handler>.Instance);

    [Fact]
    public async Task Reporting_a_spot_stores_the_report_and_notifies_admins()
    {
        await using var db = TestDbContextFactory.Create();
        var admin = await TestUserFactory.SeedAsync(db, UserRole.Admin);
        var reporter = await TestUserFactory.SeedAsync(db);
        var spot = await TestSpotFactory.SeedAsync(db);

        var result = await CreateSpotHandler(db, reporter.Id).Handle(
            new ReportSpot.Command(spot.Id, ReportReason.Spam, "  looks like an ad  "), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var report = await db.Reports.SingleAsync(r => r.Id == result.Value.Id);
        Assert.Equal(ReportedContentType.Spot, report.ContentType);
        Assert.Equal(spot.Id, report.ContentId);
        Assert.Equal(reporter.Id, report.ReporterUserId);
        Assert.Equal("looks like an ad", report.Details);
        Assert.Null(report.ResolvedAt);
        Assert.True(await db.Notifications.AnyAsync(n =>
            n.UserId == admin.Id && n.Type == NotificationType.ReportSubmitted && n.RelatedSpotId == spot.Id));
    }

    [Fact]
    public async Task Reporting_the_same_spot_twice_is_a_conflict()
    {
        await using var db = TestDbContextFactory.Create();
        var reporter = await TestUserFactory.SeedAsync(db);
        var spot = await TestSpotFactory.SeedAsync(db);
        var handler = CreateSpotHandler(db, reporter.Id);
        await handler.Handle(new ReportSpot.Command(spot.Id, ReportReason.Spam, null), CancellationToken.None);

        var result = await handler.Handle(new ReportSpot.Command(spot.Id, ReportReason.Inappropriate, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReportsMessageKeys.AlreadyReported, result.Error.Code);
        Assert.Equal(1, await db.Reports.CountAsync(r => r.ContentId == spot.Id));
    }

    [Fact]
    public async Task Different_users_can_each_report_the_same_spot()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await TestSpotFactory.SeedAsync(db);

        await CreateSpotHandler(db, Guid.NewGuid()).Handle(new ReportSpot.Command(spot.Id, ReportReason.Spam, null), CancellationToken.None);
        var result = await CreateSpotHandler(db, Guid.NewGuid()).Handle(
            new ReportSpot.Command(spot.Id, ReportReason.Spam, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, await db.Reports.CountAsync(r => r.ContentId == spot.Id));
    }

    [Fact]
    public async Task Admins_cannot_report()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await TestSpotFactory.SeedAsync(db);

        var result = await CreateSpotHandler(db, Guid.NewGuid(), UserRole.Admin).Handle(
            new ReportSpot.Command(spot.Id, ReportReason.Spam, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReportsMessageKeys.AdminsCannotReport, result.Error.Code);
        Assert.False(await db.Reports.AnyAsync(r => r.ContentId == spot.Id));
    }

    [Fact]
    public async Task Reporting_a_missing_spot_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await CreateSpotHandler(db, Guid.NewGuid()).Handle(
            new ReportSpot.Command(Guid.NewGuid(), ReportReason.Spam, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReportsMessageKeys.SpotNotFound, result.Error.Code);
    }

    [Fact]
    public async Task Reporting_a_comment_links_the_report_to_its_spot_for_admins()
    {
        await using var db = TestDbContextFactory.Create();
        var admin = await TestUserFactory.SeedAsync(db, UserRole.Admin);
        var spot = await TestSpotFactory.SeedAsync(db);
        var comment = await TestCommentFactory.SeedAsync(db, spot.Id, Guid.NewGuid());

        var result = await CreateCommentHandler(db, Guid.NewGuid()).Handle(
            new ReportComment.Command(comment.Id, ReportReason.Inappropriate, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var report = await db.Reports.SingleAsync(r => r.Id == result.Value.Id);
        Assert.Equal(ReportedContentType.Comment, report.ContentType);
        Assert.Equal(comment.Id, report.ContentId);
        Assert.True(await db.Notifications.AnyAsync(n =>
            n.UserId == admin.Id && n.Type == NotificationType.ReportSubmitted && n.RelatedSpotId == spot.Id));
    }

    [Fact]
    public async Task Reporting_a_deleted_comment_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await TestSpotFactory.SeedAsync(db);
        var comment = await TestCommentFactory.SeedAsync(db, spot.Id, Guid.NewGuid(), isDeleted: true);

        var result = await CreateCommentHandler(db, Guid.NewGuid()).Handle(
            new ReportComment.Command(comment.Id, ReportReason.Spam, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReportsMessageKeys.CommentNotFound, result.Error.Code);
    }

    [Fact]
    public void Validator_rejects_an_unknown_reason_and_overlong_details()
    {
        var validator = new ReportSpot.Validator(TestLocalizerFactory.Create());

        var result = validator.Validate(new ReportSpot.Command(Guid.NewGuid(), (ReportReason)999, new string('x', 501)));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ReportSpot.Command.Reason));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ReportSpot.Command.Details));
    }
}
