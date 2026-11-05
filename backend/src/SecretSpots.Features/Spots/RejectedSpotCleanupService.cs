using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Hosting;
using SecretSpots.Features.Common.Persistence;

namespace SecretSpots.Features.Spots;

// Soft-deletes rejected spots once their grace period (Spot.ScheduledDeletionAt) has passed and
// tells each creator. Runs every SpotModeration:CleanupIntervalMinutes (see PeriodicBackgroundService).
public class RejectedSpotCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<SpotModerationOptions> options,
    TimeProvider timeProvider,
    ILogger<RejectedSpotCleanupService> logger) : PeriodicBackgroundService(scopeFactory, logger)
{
    protected override TimeSpan Interval => TimeSpan.FromMinutes(options.Value.CleanupIntervalMinutes);

    protected override async Task RunOnceAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<IAppDbContext>();

        var now = timeProvider.GetUtcNow();

        var expired = await db.Spots
            .Where(s => s.ModerationStatus == SpotModerationStatus.Rejected && s.ScheduledDeletionAt <= now)
            .Select(s => new { s.Id, s.CreatedByUserId, s.Name })
            .ToListAsync(cancellationToken);
        if (expired.Count == 0)
        {
            return;
        }

        // One transaction: a spot is never deleted without its notification or the other way round.
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);
        var deletedCount = 0;
        foreach (var spot in expired)
        {
            // Re-checks the status in the UPDATE itself: an admin may have restored the spot since it
            // was read above, and then it must be neither deleted nor announced as deleted. Already
            // soft-deleted spots are excluded by the global query filter.
            var updated = await db.Spots
                .Where(s => s.Id == spot.Id && s.ModerationStatus == SpotModerationStatus.Rejected)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.DeletedAt, now), cancellationToken);
            if (updated == 0)
            {
                continue;
            }

            deletedCount++;
            // No spot link: the spot is hidden now, even from its creator; only its name is left.
            db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = spot.CreatedByUserId,
                Type = NotificationType.RejectedSpotDeleted,
                RelatedSpotName = spot.Name,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        if (deletedCount > 0)
        {
            Logger.LogInformation(SpotsLogMessages.RejectedSpotsSoftDeleted, deletedCount);
        }
    }
}
