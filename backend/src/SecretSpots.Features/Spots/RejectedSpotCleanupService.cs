using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Hosting;
using SecretSpots.Features.Common.Persistence;

namespace SecretSpots.Features.Spots;

// Soft-deletes rejected spots once their grace period (Spot.ScheduledDeletionAt) has passed.
// Runs every SpotModeration:CleanupIntervalMinutes (see PeriodicBackgroundService).
public class RejectedSpotCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<SpotModerationOptions> options,
    ILogger<RejectedSpotCleanupService> logger) : PeriodicBackgroundService(scopeFactory, logger)
{
    protected override TimeSpan Interval => TimeSpan.FromMinutes(options.Value.CleanupIntervalMinutes);

    protected override async Task RunOnceAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<IAppDbContext>();

        var now = DateTimeOffset.UtcNow;

        // The ModerationStatus check is a safety net — ApproveSpot already clears
        // ScheduledDeletionAt. Already soft-deleted spots are excluded by the global query filter.
        var deletedCount = await db.Spots
            .Where(s => s.ModerationStatus == SpotModerationStatus.Rejected && s.ScheduledDeletionAt <= now)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.DeletedAt, now), cancellationToken);

        if (deletedCount > 0)
        {
            Logger.LogInformation(SpotsLogMessages.RejectedSpotsSoftDeleted, deletedCount);
        }
    }
}
