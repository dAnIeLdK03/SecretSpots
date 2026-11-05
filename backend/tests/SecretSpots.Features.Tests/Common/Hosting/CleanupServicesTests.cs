using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using SecretSpots.Domain;
using SecretSpots.Features.Admin;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Spots;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Common.Hosting;

[Collection(SharedDatabaseCleanupCollection.Name)]
public class CleanupServicesTests
{
    private static ServiceProvider CreateServices() =>
        new ServiceCollection().AddScoped<IAppDbContext>(_ => TestDbContextFactory.Create()).BuildServiceProvider();

    [Fact]
    public async Task Rejected_spots_are_soft_deleted_only_once_their_grace_period_has_passed()
    {
        await using var db = TestDbContextFactory.Create();
        var expired = await TestSpotFactory.SeedAsync(db, status: SpotModerationStatus.Rejected);
        var stillInGrace = await TestSpotFactory.SeedAsync(db, status: SpotModerationStatus.Rejected);
        // Safety net: an approved spot with a stale deadline must never be removed.
        var approvedWithStaleDeadline = await TestSpotFactory.SeedAsync(db, status: SpotModerationStatus.Approved);
        expired.ScheduledDeletionAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        stillInGrace.ScheduledDeletionAt = DateTimeOffset.UtcNow.AddHours(1);
        approvedWithStaleDeadline.ScheduledDeletionAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();
        await using var services = CreateServices();

        await new TestableRejectedSpotCleanupService(services).RunOnceAsync();

        db.ChangeTracker.Clear();
        var remaining = await db.Spots.IgnoreQueryFilters()
            .Where(s => s.Id == expired.Id || s.Id == stillInGrace.Id || s.Id == approvedWithStaleDeadline.Id)
            .ToDictionaryAsync(s => s.Id, s => s.DeletedAt);
        Assert.Equal(3, remaining.Count);
        Assert.NotNull(remaining[expired.Id]);
        Assert.Null(remaining[stillInGrace.Id]);
        Assert.Null(remaining[approvedWithStaleDeadline.Id]);
        Assert.False(await db.Spots.AnyAsync(s => s.Id == expired.Id));
        Assert.Equal([expired.CreatedByUserId], await DeletedNotificationOwnersAsync(
            db, expired.CreatedByUserId, stillInGrace.CreatedByUserId, approvedWithStaleDeadline.CreatedByUserId));
    }

    private static Task<List<Guid>> DeletedNotificationOwnersAsync(IAppDbContext db, params Guid[] userIds) =>
        db.Notifications
            .Where(n => userIds.Contains(n.UserId) && n.Type == NotificationType.RejectedSpotDeleted)
            .Select(n => n.UserId)
            .ToListAsync();

    [Fact]
    public async Task A_rejected_spot_survives_its_grace_period_and_is_soft_deleted_after_it()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await TestSpotFactory.SeedAsync(db, status: SpotModerationStatus.Pending);
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        await new RejectSpot.Handler(
                db, new FakeUserContext(Guid.NewGuid(), UserRole.Admin),
                Options.Create(new SpotModerationOptions { RejectionGracePeriodHours = 24 }),
                time, TestLocalizerFactory.Create(), NullLogger<RejectSpot.Handler>.Instance)
            .Handle(new RejectSpot.Command(spot.Id, "Blurry photo"), CancellationToken.None);
        await using var services = CreateServices();
        var cleanup = new TestableRejectedSpotCleanupService(services, time);

        time.Advance(TimeSpan.FromHours(23));
        await cleanup.RunOnceAsync();
        db.ChangeTracker.Clear();
        var stillThere = await db.Spots.AnyAsync(s => s.Id == spot.Id);
        var notifiedEarly = (await DeletedNotificationOwnersAsync(db, spot.CreatedByUserId)).Count;
        time.Advance(TimeSpan.FromHours(2));
        await cleanup.RunOnceAsync();
        // A later run finds nothing left to delete, so it doesn't notify again.
        await cleanup.RunOnceAsync();
        db.ChangeTracker.Clear();

        Assert.True(stillThere);
        Assert.Equal(0, notifiedEarly);
        Assert.False(await db.Spots.AnyAsync(s => s.Id == spot.Id));
        Assert.NotNull((await db.Spots.IgnoreQueryFilters().SingleAsync(s => s.Id == spot.Id)).DeletedAt);
        var notification = await db.Notifications.SingleAsync(n =>
            n.UserId == spot.CreatedByUserId && n.Type == NotificationType.RejectedSpotDeleted);
        Assert.Null(notification.RelatedSpotId);
        Assert.Equal(spot.Name, notification.RelatedSpotName);
    }

    [Fact]
    public async Task A_spot_restored_within_its_grace_period_is_neither_deleted_nor_announced()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await TestSpotFactory.SeedAsync(db, status: SpotModerationStatus.Pending);
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var admin = new FakeUserContext(Guid.NewGuid(), UserRole.Admin);
        await new RejectSpot.Handler(
                db, admin, Options.Create(new SpotModerationOptions { RejectionGracePeriodHours = 24 }),
                time, TestLocalizerFactory.Create(), NullLogger<RejectSpot.Handler>.Instance)
            .Handle(new RejectSpot.Command(spot.Id, "Blurry photo"), CancellationToken.None);
        time.Advance(TimeSpan.FromHours(12));
        await new ApproveSpot.Handler(db, admin, TestLocalizerFactory.Create(), NullLogger<ApproveSpot.Handler>.Instance)
            .Handle(new ApproveSpot.Command(spot.Id), CancellationToken.None);
        await using var services = CreateServices();

        time.Advance(TimeSpan.FromHours(13));
        await new TestableRejectedSpotCleanupService(services, time).RunOnceAsync();
        db.ChangeTracker.Clear();

        Assert.True(await db.Spots.AnyAsync(s => s.Id == spot.Id));
        Assert.Empty(await DeletedNotificationOwnersAsync(db, spot.CreatedByUserId));
    }

    [Fact]
    public async Task Token_cleanup_removes_expired_and_spent_tokens_and_keeps_live_ones()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        RefreshToken Refresh(DateTimeOffset expiresAt, DateTimeOffset? revokedAt = null) => new()
        {
            Id = Guid.NewGuid(), UserId = userId, Token = Guid.NewGuid().ToString("N"), ExpiresAt = expiresAt, RevokedAt = revokedAt,
        };
        PasswordResetToken Reset(DateTimeOffset expiresAt, DateTimeOffset? usedAt = null) => new()
        {
            Id = Guid.NewGuid(), UserId = userId, Token = Guid.NewGuid().ToString("N"), ExpiresAt = expiresAt, UsedAt = usedAt,
        };
        ExternalAuthTransaction Transaction(DateTimeOffset expiresAt, ExternalAuthTransactionStatus status) => new()
        {
            Id = Guid.NewGuid().ToString("N"), Provider = ExternalAuthProvider.Google, ExpiresAt = expiresAt, Status = status,
        };
        var now = DateTimeOffset.UtcNow;
        var liveRefresh = Refresh(now.AddDays(1));
        var expiredRefresh = Refresh(now.AddMinutes(-1));
        var revokedRefresh = Refresh(now.AddDays(1), revokedAt: now);
        var liveReset = Reset(now.AddHours(1));
        var usedReset = Reset(now.AddHours(1), usedAt: now);
        var pendingTransaction = Transaction(now.AddMinutes(5), ExternalAuthTransactionStatus.Pending);
        var consumedTransaction = Transaction(now.AddMinutes(5), ExternalAuthTransactionStatus.Consumed);
        db.RefreshTokens.AddRange(liveRefresh, expiredRefresh, revokedRefresh);
        db.PasswordResetTokens.AddRange(liveReset, usedReset);
        db.ExternalAuthTransactions.AddRange(pendingTransaction, consumedTransaction);
        await db.SaveChangesAsync();
        await using var services = CreateServices();

        await new TestableTokenCleanupService(services).RunOnceAsync();

        db.ChangeTracker.Clear();
        Assert.Equal([liveRefresh.Id], await db.RefreshTokens.Where(t => t.UserId == userId).Select(t => t.Id).ToListAsync());
        Assert.Equal([liveReset.Id], await db.PasswordResetTokens.Where(t => t.UserId == userId).Select(t => t.Id).ToListAsync());
        Assert.True(await db.ExternalAuthTransactions.AnyAsync(t => t.Id == pendingTransaction.Id));
        Assert.False(await db.ExternalAuthTransactions.AnyAsync(t => t.Id == consumedTransaction.Id));
    }

    // RunOnceAsync is protected — these expose a single run without starting the timer loop.
    private sealed class TestableRejectedSpotCleanupService(IServiceProvider services, TimeProvider? time = null)
        : RejectedSpotCleanupService(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new SpotModerationOptions()),
            time ?? TimeProvider.System,
            NullLogger<RejectedSpotCleanupService>.Instance)
    {
        public async Task RunOnceAsync()
        {
            using var scope = services.CreateScope();
            await RunOnceAsync(scope.ServiceProvider, CancellationToken.None);
        }
    }

    private sealed class TestableTokenCleanupService(IServiceProvider services)
        : TokenCleanupService(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new TokenCleanupOptions()),
            TimeProvider.System,
            NullLogger<TokenCleanupService>.Instance)
    {
        public async Task RunOnceAsync()
        {
            using var scope = services.CreateScope();
            await RunOnceAsync(scope.ServiceProvider, CancellationToken.None);
        }
    }
}
