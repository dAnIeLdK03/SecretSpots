using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Persistence;

namespace SecretSpots.Features.Spots;

// Soft-deletes rejected spots once their grace period (Spot.ScheduledDeletionAt) has passed.
// Same timer-for-the-process-lifetime approach as TokenCleanupService, for the same reason.
public class RejectedSpotCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<SpotModerationOptions> options,
    ILogger<RejectedSpotCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(options.Value.CleanupIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupAsync(stoppingToken);

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var now = DateTimeOffset.UtcNow;

        // The ModerationStatus check is a safety net — ApproveSpot already clears
        // ScheduledDeletionAt. Already soft-deleted spots are excluded by the global query filter.
        var deletedCount = await db.Spots
            .Where(s => s.ModerationStatus == SpotModerationStatus.Rejected && s.ScheduledDeletionAt <= now)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.DeletedAt, now), cancellationToken);

        if (deletedCount > 0)
        {
            logger.LogInformation(SpotsLogMessages.RejectedSpotsSoftDeleted, deletedCount);
        }
    }
}
