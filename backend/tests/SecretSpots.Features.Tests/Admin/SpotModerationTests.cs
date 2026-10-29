using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Admin;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Admin;

// Approve / reject (= remove when approved) / restore, plus the admin spot list and counts.
public class SpotModerationTests
{
    private const int GracePeriodHours = 24;

    private static ApproveSpot.Handler CreateApproveHandler(IAppDbContext db) =>
        new(db, new FakeUserContext(Guid.NewGuid(), UserRole.Admin), TestLocalizerFactory.Create(), NullLogger<ApproveSpot.Handler>.Instance);

    private static RejectSpot.Handler CreateRejectHandler(IAppDbContext db) =>
        new(db, new FakeUserContext(Guid.NewGuid(), UserRole.Admin),
            Options.Create(new SpotModerationOptions { RejectionGracePeriodHours = GracePeriodHours }),
            TestLocalizerFactory.Create(), NullLogger<RejectSpot.Handler>.Instance);

    private static Task<Notification[]> NotificationsForAsync(IAppDbContext db, Spot spot) =>
        db.Notifications.Where(n => n.UserId == spot.CreatedByUserId).ToArrayAsync();

    [Fact]
    public async Task Approving_a_pending_spot_publishes_it_and_notifies_the_creator()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await TestSpotFactory.SeedAsync(db, status: SpotModerationStatus.Pending);

        var result = await CreateApproveHandler(db).Handle(new ApproveSpot.Command(spot.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        db.ChangeTracker.Clear();
        Assert.Equal(SpotModerationStatus.Approved, (await db.Spots.SingleAsync(s => s.Id == spot.Id)).ModerationStatus);
        var notification = Assert.Single(await NotificationsForAsync(db, spot));
        Assert.Equal(NotificationType.SpotApproved, notification.Type);
        Assert.Equal(spot.Name, notification.RelatedSpotName);
    }

    [Fact]
    public async Task Rejecting_a_pending_spot_stores_the_reason_and_schedules_deletion()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await TestSpotFactory.SeedAsync(db, status: SpotModerationStatus.Pending);
        var before = DateTimeOffset.UtcNow;

        var result = await CreateRejectHandler(db).Handle(new RejectSpot.Command(spot.Id, "  Blurry photo  "), CancellationToken.None);

        Assert.True(result.IsSuccess);
        db.ChangeTracker.Clear();
        var saved = await db.Spots.SingleAsync(s => s.Id == spot.Id);
        Assert.Equal(SpotModerationStatus.Rejected, saved.ModerationStatus);
        Assert.Equal("Blurry photo", saved.RejectionReason);
        Assert.NotNull(saved.ScheduledDeletionAt);
        Assert.InRange(saved.ScheduledDeletionAt!.Value, before.AddHours(GracePeriodHours), DateTimeOffset.UtcNow.AddHours(GracePeriodHours));
        Assert.Equal(NotificationType.SpotRejected, Assert.Single(await NotificationsForAsync(db, spot)).Type);
    }

    [Fact]
    public async Task Rejecting_an_approved_spot_is_a_removal()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await TestSpotFactory.SeedAsync(db, status: SpotModerationStatus.Approved);

        await CreateRejectHandler(db).Handle(new RejectSpot.Command(spot.Id, "Private property"), CancellationToken.None);

        Assert.Equal(NotificationType.SpotRemoved, Assert.Single(await NotificationsForAsync(db, spot)).Type);
    }

    [Fact]
    public async Task Re_rejecting_updates_the_reason_but_keeps_the_grace_period_and_does_not_notify_again()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await TestSpotFactory.SeedAsync(db, status: SpotModerationStatus.Pending);
        var handler = CreateRejectHandler(db);
        await handler.Handle(new RejectSpot.Command(spot.Id, "First wording"), CancellationToken.None);
        db.ChangeTracker.Clear();
        var firstDeadline = (await db.Spots.SingleAsync(s => s.Id == spot.Id)).ScheduledDeletionAt;

        await handler.Handle(new RejectSpot.Command(spot.Id, "Better wording"), CancellationToken.None);

        db.ChangeTracker.Clear();
        var saved = await db.Spots.SingleAsync(s => s.Id == spot.Id);
        Assert.Equal("Better wording", saved.RejectionReason);
        Assert.Equal(firstDeadline, saved.ScheduledDeletionAt);
        Assert.Single(await NotificationsForAsync(db, spot));
    }

    [Fact]
    public async Task Approving_a_rejected_spot_restores_it_and_cancels_the_deletion()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await TestSpotFactory.SeedAsync(db, status: SpotModerationStatus.Pending);
        await CreateRejectHandler(db).Handle(new RejectSpot.Command(spot.Id, "Wrong category"), CancellationToken.None);

        await CreateApproveHandler(db).Handle(new ApproveSpot.Command(spot.Id), CancellationToken.None);

        db.ChangeTracker.Clear();
        var saved = await db.Spots.SingleAsync(s => s.Id == spot.Id);
        Assert.Equal(SpotModerationStatus.Approved, saved.ModerationStatus);
        Assert.Null(saved.RejectionReason);
        Assert.Null(saved.ScheduledDeletionAt);
        Assert.Contains(await NotificationsForAsync(db, spot), n => n.Type == NotificationType.SpotRestored);
    }

    [Fact]
    public async Task Approving_an_already_approved_spot_does_not_notify_again()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await TestSpotFactory.SeedAsync(db, status: SpotModerationStatus.Approved);

        var result = await CreateApproveHandler(db).Handle(new ApproveSpot.Command(spot.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(await NotificationsForAsync(db, spot));
    }

    [Fact]
    public async Task Moderating_a_missing_spot_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();

        var approve = await CreateApproveHandler(db).Handle(new ApproveSpot.Command(Guid.NewGuid()), CancellationToken.None);
        var reject = await CreateRejectHandler(db).Handle(new RejectSpot.Command(Guid.NewGuid(), "x"), CancellationToken.None);

        Assert.Equal(AdminMessageKeys.SpotNotFound, approve.Error.Code);
        Assert.Equal(AdminMessageKeys.SpotNotFound, reject.Error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Reject_validator_requires_a_reason(string reason)
    {
        var result = new RejectSpot.Validator(TestLocalizerFactory.Create()).Validate(new RejectSpot.Command(Guid.NewGuid(), reason));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Reject_validator_caps_the_reason_length()
    {
        var validator = new RejectSpot.Validator(TestLocalizerFactory.Create());

        Assert.True(validator.Validate(new RejectSpot.Command(Guid.NewGuid(), new string('x', RejectionReasonRules.MaxLength))).IsValid);
        Assert.False(validator.Validate(new RejectSpot.Command(Guid.NewGuid(), new string('x', RejectionReasonRules.MaxLength + 1))).IsValid);
    }

    [Fact]
    public async Task Admin_spot_list_filters_by_status_creator_and_search_term()
    {
        await using var db = TestDbContextFactory.Create();
        var marker = Guid.NewGuid().ToString("N");
        var creator = await TestUserFactory.SeedAsync(db, displayName: $"Creator {marker}");
        var pending = await TestSpotFactory.SeedAsync(db, creator.Id, SpotModerationStatus.Pending, name: $"Lake {marker}");
        var approved = await TestSpotFactory.SeedAsync(db, creator.Id, SpotModerationStatus.Approved, name: $"Hill {marker}");
        var deleted = await TestSpotFactory.SeedAsync(db, creator.Id, SpotModerationStatus.Rejected, name: $"Gone {marker}");
        deleted.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new GetAdminSpots.Handler(db);

        var byCreator = await handler.Handle(new GetAdminSpots.Query(null, creator.Id, null, 1, 50), CancellationToken.None);
        var pendingOnly = await handler.Handle(new GetAdminSpots.Query(SpotModerationStatus.Pending, creator.Id, null, 1, 50), CancellationToken.None);
        var byName = await handler.Handle(new GetAdminSpots.Query(null, null, $"LAKE {marker}", 1, 50), CancellationToken.None);
        var byCreatorName = await handler.Handle(new GetAdminSpots.Query(null, null, $"creator {marker}", 1, 50), CancellationToken.None);

        Assert.Equal([approved.Id, pending.Id], byCreator.Items.Select(s => s.Id));
        Assert.Equal([pending.Id], pendingOnly.Items.Select(s => s.Id));
        Assert.Equal([pending.Id], byName.Items.Select(s => s.Id));
        Assert.Equal(2, byCreatorName.TotalCount);
        Assert.All(byCreator.Items, s => Assert.Equal(creator.DisplayName, s.CreatedByDisplayName));
    }

    [Fact]
    public async Task Admin_spot_counts_go_up_by_status()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetAdminSpotCounts.Handler(db);
        // Other test classes add spots in parallel, so only assert a lower bound on the delta.
        var before = await handler.Handle(new GetAdminSpotCounts.Query(), CancellationToken.None);
        await TestSpotFactory.SeedAsync(db, status: SpotModerationStatus.Pending);
        await TestSpotFactory.SeedAsync(db, status: SpotModerationStatus.Rejected);

        var after = await handler.Handle(new GetAdminSpotCounts.Query(), CancellationToken.None);

        Assert.True(after.Pending >= before.Pending + 1);
        Assert.True(after.Rejected >= before.Rejected + 1);
    }
}
