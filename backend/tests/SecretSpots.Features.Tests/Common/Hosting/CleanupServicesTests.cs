using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
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
    private sealed class TestableRejectedSpotCleanupService(IServiceProvider services)
        : RejectedSpotCleanupService(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new SpotModerationOptions()),
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
            NullLogger<TokenCleanupService>.Instance)
    {
        public async Task RunOnceAsync()
        {
            using var scope = services.CreateScope();
            await RunOnceAsync(scope.ServiceProvider, CancellationToken.None);
        }
    }
}
